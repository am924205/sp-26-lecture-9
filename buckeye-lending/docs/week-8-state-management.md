# Week 8 — State Management: `useReducer` + Context API

## Overview

The Buckeye Lending frontend was refactored from a collection of scattered `useState` calls and prop-drilled handlers into a single, centralized state layer built on `useReducer` and the React Context API.

---

## What Changed

### Before

`App.tsx` owned all state directly using four separate `useState` hooks:

```tsx
const [loans, setLoans] = useState<LoanApplication[]>([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState<string | null>(null);
const [selectedType, setSelectedType] = useState("All");
```

`LoanApplicationCard` rendered a static, non-interactive "View Details" button. There were no user actions on individual loans.

### After

All state lives in a single `LoanDashboardState` object managed by `useReducer`. A `LoanProvider` context component wraps the app and makes that state available to any descendant — no prop drilling required. `LoanApplicationCard` now renders three action buttons (Approve, Deny, Flag) that each dispatch directly to the reducer.

---

## New File Structure

```
src/
  types/
    loanActions.ts          ← state shape + action type definitions
  reducers/
    loanReducer.ts          ← pure reducer function
  contexts/
    LoanContext.tsx          ← Provider + useLoanContext() hook
  components/
    ActionButtons.tsx        ← Approve/Deny/Flag buttons (reads context directly)
```

---

## `useReducer`

### What it is

`useReducer` is a React hook that manages state through a **reducer function** — a pure function that takes the current state and an action, and returns the next state. It is an alternative to `useState` suited for state that involves multiple sub-values or where transitions follow defined rules.

```tsx
const [state, dispatch] = useReducer(loanReducer, initialState);
```

- `state` — the current `LoanDashboardState` object
- `dispatch` — a function used to send actions to the reducer
- `loanReducer` — the function that defines how state changes
- `initialState` — the starting value of state

### The State Shape

Defined in `src/types/loanActions.ts`:

```ts
export type LoanDashboardState = {
  loans: LoanApplication[]; // full list from the API
  filter: string; // active loan-type filter ("All" or a type name)
  loading: boolean; // is the API request in flight?
  error: string | null; // API error message, if any
  notificationCount: number; // increments on every status-changing action
};
```

All state that was previously spread across four `useState` calls is now a single, structured object. This means every piece of state changes atomically and is always internally consistent.

### The Action Union

Also in `src/types/loanActions.ts`:

```ts
export type LoanAction =
  | { type: "FETCH_START" }
  | { type: "FETCH_SUCCESS"; loans: LoanApplication[] }
  | { type: "FETCH_ERROR"; message: string }
  | { type: "APPROVE_LOAN"; loanId: number }
  | { type: "DENY_LOAN"; loanId: number }
  | { type: "FLAG_LOAN"; loanId: number }
  | { type: "SET_FILTER"; filter: string }
  | { type: "CLEAR_NOTIFICATIONS" };
```

This is a **discriminated union** — every possible state transition is named and explicitly typed. TypeScript enforces that every action includes the correct payload. If you add a new action type and forget to handle it in the reducer, you get a compile error.

### The Reducer

Defined in `src/reducers/loanReducer.ts`:

```ts
export function loanReducer(
  state: LoanDashboardState,
  action: LoanAction,
): LoanDashboardState {
  switch (action.type) {
    case "FETCH_START":
      return { ...state, loading: true, error: null };

    case "FETCH_SUCCESS":
      return { ...state, loading: false, loans: action.loans };

    case "FETCH_ERROR":
      return { ...state, loading: false, error: action.message };

    case "APPROVE_LOAN":
      return {
        ...state,
        loans: state.loans.map((loan) =>
          loan.id === action.loanId ? { ...loan, status: "Approved" } : loan,
        ),
        notificationCount: state.notificationCount + 1,
      };

    case "DENY_LOAN": // same pattern — status + notificationCount
    case "FLAG_LOAN": // same pattern — status + notificationCount
    case "SET_FILTER":
      return { ...state, filter: action.filter };

    case "CLEAR_NOTIFICATIONS":
      return { ...state, notificationCount: 0 };
  }
}
```

Key properties of this reducer:

- **Pure function** — given the same inputs, always returns the same output. No side effects.
- **Immutable updates** — every case returns a new object using the spread operator (`...state`). The original state is never mutated.
- **No `default` case** — TypeScript's exhaustive checking ensures every action in `LoanAction` is handled. A missing case is a compile error, not a silent bug.
- **Related state changes in one place** — every loan status change (`APPROVE_LOAN`, `DENY_LOAN`, `FLAG_LOAN`) also increments `notificationCount`. That relationship is codified here, once, rather than repeated across multiple handler functions.

---

## Context API

### What it is

The React Context API provides a way to pass data through the component tree without manually passing props at every level. It is the mechanism used to make `state` and `dispatch` available to any component in the app — however deeply nested.

### The Provider

Defined in `src/contexts/LoanContext.tsx`:

```tsx
export function LoanProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(loanReducer, initialState);

  // Side-effect: fetch loans from the API on mount
  useEffect(() => {
    dispatch({ type: "FETCH_START" });
    fetchLoanApplications()
      .then((loans) => dispatch({ type: "FETCH_SUCCESS", loans }))
      .catch((err) => dispatch({ type: "FETCH_ERROR", message: err.message }));
  }, []);

  // Derived values — computed from state, not stored separately
  const loanTypes = [
    "All",
    ...new Set(state.loans.map((l) => l.loanType.name)),
  ];
  const filteredLoans =
    state.filter === "All"
      ? state.loans
      : state.loans.filter((l) => l.loanType.name === state.filter);

  return (
    <LoanContext.Provider value={{ state, dispatch, filteredLoans, loanTypes }}>
      {children}
    </LoanContext.Provider>
  );
}
```

`LoanProvider` is responsible for three things:

1. Creating and owning the `useReducer` instance
2. Running the initial API fetch as a `useEffect` (dispatching actions rather than calling `setState`)
3. Computing derived values (`filteredLoans`, `loanTypes`) and exposing them alongside raw state

### The Custom Hook

```tsx
export function useLoanContext() {
  const context = useContext(LoanContext);
  if (!context) {
    throw new Error("useLoanContext must be used within a LoanProvider");
  }
  return context;
}
```

`useLoanContext()` is the consumer API. Any component calls it to access `state`, `dispatch`, `filteredLoans`, or `loanTypes`. The guard clause (`if (!context)`) produces a clear, actionable error if a component accidentally renders outside the provider tree.

### How Components Use It

**`App.tsx`** — wraps the tree with the provider; `Dashboard` reads context directly:

```tsx
function App() {
  return (
    <LoanProvider>
      <Dashboard />
    </LoanProvider>
  );
}
```

**`LoanApplicationList.tsx`** — no longer accepts a `loans` prop:

```tsx
function LoanApplicationList() {
  const { filteredLoans } = useLoanContext();
  // ...
}
```

**`ActionButtons.tsx`** — dispatches actions without receiving any handlers as props:

```tsx
function ActionButtons({ loanId, currentStatus }: ActionButtonsProps) {
  const { dispatch } = useLoanContext();

  return (
    <div>
      <button onClick={() => dispatch({ type: "APPROVE_LOAN", loanId })}>
        Approve
      </button>
      <button onClick={() => dispatch({ type: "DENY_LOAN", loanId })}>
        Deny
      </button>
      <button onClick={() => dispatch({ type: "FLAG_LOAN", loanId })}>
        Flag
      </button>
    </div>
  );
}
```

---

## Component Tree: Before vs. After

### Before — prop drilling

```
App (owns state + handlers)
  └── LoanApplicationList (loans prop)
        └── LoanApplicationCard (loan prop)
              └── [static button — no actions]
```

### After — context

```
LoanProvider (owns state + dispatch)
  └── Dashboard (reads context)
        └── LoanApplicationList (reads context)
              └── LoanApplicationCard (loan prop only)
                    └── ActionButtons (reads context — dispatches directly)
```

Intermediate components (`LoanApplicationList`, `LoanApplicationCard`) no longer carry props they don't use. Each component only receives what it actually needs.

---

## Why It Matters

| Concern               | Before                                                              | After                                                                 |
| --------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **State location**    | 4 `useState` calls in `App`                                         | 1 `useReducer` in `LoanProvider`                                      |
| **State transitions** | Inline arrow functions scattered in JSX                             | Named, typed actions in the reducer                                   |
| **Side effects**      | `useEffect` calling `setLoans`, `setLoading`, `setError` separately | `useEffect` dispatching `FETCH_START`, `FETCH_SUCCESS`, `FETCH_ERROR` |
| **Related changes**   | `notificationCount` would need updating in 3 separate handlers      | Handled once in the reducer — impossible to forget                    |
| **Prop drilling**     | `onApprove`/`onDeny`/`onFlag` passed through every layer            | Eliminated — leaf components read context directly                    |
| **Testability**       | Handler logic is embedded in component closures                     | `loanReducer` is a pure function — trivial to unit test               |
| **TypeScript safety** | `setState` calls can set any value                                  | Dispatcher only accepts the defined `LoanAction` union                |
