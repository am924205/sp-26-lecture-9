### Starting Point (Pre-built, just walk through — 1 min)

Show the existing `LoanDashboard.tsx` with multiple `useState` calls, repetitive handlers, and props being drilled through `LoanList` → `LoanCard` → `ActionButtons`.

**Narrate:** "Here's our starting point. A working loan dashboard. I've got four `useState` calls, five handler functions that all look similar, and I'm passing `onApprove`, `onDeny`, and `onFlag` through three levels of components. It works, but let's make it better."

**[Show the app running in browser — click approve/deny, show it working]**

### Step 1: Define the Action Types and State Type (1 min)

Create `types/loanActions.ts`:

```tsx
export type LoanDashboardState = {
  loans: LoanApplication[];
  filter: string;
  notificationCount: number;
};

export type LoanAction =
  | { type: "APPROVE_LOAN"; loanId: number }
  | { type: "DENY_LOAN"; loanId: number }
  | { type: "FLAG_LOAN"; loanId: number }
  | { type: "SET_FILTER"; filter: string }
  | { type: "CLEAR_NOTIFICATIONS" };
```

**Narrate:** "First, I define what my state looks like and what actions can happen. This is like writing a contract: here are the only things that can change state in my dashboard. TypeScript will enforce this — if I try to dispatch an action that doesn't match, I get a compile error."

### Step 2: Write the Reducer (2 min)

Create `reducers/loanReducer.ts`:

```tsx
export function loanReducer(
  state: LoanDashboardState,
  action: LoanAction,
): LoanDashboardState {
  switch (action.type) {
    case "APPROVE_LOAN":
      return {
        ...state,
        loans: state.loans.map((loan) =>
          loan.id === action.loanId ? { ...loan, status: "Approved" } : loan,
        ),
        notificationCount: state.notificationCount + 1,
      };
    case "DENY_LOAN":
      return {
        ...state,
        loans: state.loans.map((loan) =>
          loan.id === action.loanId ? { ...loan, status: "Denied" } : loan,
        ),
        notificationCount: state.notificationCount + 1,
      };
    case "FLAG_LOAN":
      return {
        ...state,
        loans: state.loans.map((loan) =>
          loan.id === action.loanId ? { ...loan, status: "Flagged" } : loan,
        ),
        notificationCount: state.notificationCount + 1,
      };
    case "SET_FILTER":
      return { ...state, filter: action.filter };
    case "CLEAR_NOTIFICATIONS":
      return { ...state, notificationCount: 0 };
  }
}
```

**Narrate:** "The reducer is a switch on the action type. Each case returns a new state object. Notice — every loan status change also bumps the notification count. This logic is all in one place. If I want to add logging, analytics, or validation later, I add it here once.

Also notice: I'm not using a `default` case. TypeScript's exhaustive checking means if I add a new action type and forget to handle it here, I get a compile error. That's by design."

### Step 3: Replace useState with useReducer in the Component (2 min)

Refactor `LoanDashboard.tsx`:

```tsx
// Before: four useState calls + five handlers
// After:
function LoanDashboard() {
  const [state, dispatch] = useReducer(loanReducer, {
    loans: initialLoans,
    filter: "All",
    notificationCount: 0,
  });

  const filteredLoans =
    state.filter === "All"
      ? state.loans
      : state.loans.filter((loan) => loan.status === state.filter);

  return (
    <div>
      <Header
        notificationCount={state.notificationCount}
        onClearNotifications={() => dispatch({ type: "CLEAR_NOTIFICATIONS" })}
      />
      <FilterBar
        currentFilter={state.filter}
        onFilterChange={(f) => dispatch({ type: "SET_FILTER", filter: f })}
      />
      <LoanList
        loans={filteredLoans}
        onApprove={(id) => dispatch({ type: "APPROVE_LOAN", loanId: id })}
        onDeny={(id) => dispatch({ type: "DENY_LOAN", loanId: id })}
        onFlag={(id) => dispatch({ type: "FLAG_LOAN", loanId: id })}
      />
    </div>
  );
}
```

**Narrate:** "Four `useState` calls become one `useReducer`. Five handler functions become inline dispatches. The component is now focused on _what to render_, not _how state changes_. That logic is in the reducer.

But — we still have prop drilling. `onApprove`, `onDeny`, `onFlag` are being passed to `LoanList` just so it can pass them to `LoanCard`. Let's fix that."

**[Save, show browser still works — approve a loan, check notification count increments]**

### Step 4: Extract to Context (3 min)

Create `contexts/LoanContext.tsx`:

```tsx
import { createContext, useContext, useReducer, ReactNode } from "react";
import { loanReducer } from "../reducers/loanReducer";
import { LoanDashboardState, LoanAction } from "../types/loanActions";
import { initialLoans } from "../data/loanApplications";

type LoanContextType = {
  state: LoanDashboardState;
  dispatch: React.Dispatch<LoanAction>;
};

const LoanContext = createContext<LoanContextType | null>(null);

export function LoanProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(loanReducer, {
    loans: initialLoans,
    filter: "All",
    notificationCount: 0,
  });

  return (
    <LoanContext.Provider value={{ state, dispatch }}>
      {children}
    </LoanContext.Provider>
  );
}

export function useLoanContext() {
  const context = useContext(LoanContext);
  if (!context) {
    throw new Error("useLoanContext must be used within a LoanProvider");
  }
  return context;
}
```

**Narrate:** "Now I'm moving the useReducer into a context provider. This component holds the state and makes it available to any child. The custom hook `useLoanContext` is the API — any component calls it to access state or dispatch."

### Step 5: Simplify the Components (2 min)

Update `App.tsx`:

```tsx
function App() {
  return (
    <LoanProvider>
      <Header />
      <Sidebar />
      <MainContent />
    </LoanProvider>
  );
}
```

Update `ActionButtons.tsx`:

```tsx
function ActionButtons({ loanId }: { loanId: number }) {
  const { dispatch } = useLoanContext();
  return (
    <div>
      <button onClick={() => dispatch({ type: "APPROVE_LOAN", loanId })}>
        Approve
      </button>
      <button onClick={() => dispatch({ type: "DENY_LOAN", loanId })}>
        Deny
      </button>
    </div>
  );
}
```

**Narrate:** "Look at `App` now — no handler props at all. Clean. And `ActionButtons` reads directly from context. The intermediate components — `MainContent`, `LoanList` — no longer carry props they don't use.

Let me remove those pass-through props from `LoanList` and `LoanCard`..."

**[Remove the onApprove/onDeny/onFlag props from LoanList and LoanCard]**
**[Save, show browser, verify everything still works]**

### Step 6: Show React DevTools (1 min)
