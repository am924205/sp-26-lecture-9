# Week 8 — State Management: `useReducer` + Context API

## Overview

The OSU Event Finder frontend was refactored from a static, data-only display into a fully interactive ticket selection app. The refactor introduced `useReducer` for centralized state management and the React Context API to eliminate prop drilling — following the same pattern as Buckeye Lending but applied to a different problem domain: a shopping-cart-style ticket counter.

---

## What Changed

### Before

`App.tsx` rendered a hardcoded static list with no interactivity:

```tsx
import { events } from "./data/events";

function App() {
  return (
    <div className="app">
      <h1>OSU Event Finder</h1>
      <EventList events={events} />
    </div>
  );
}
```

`EventList` rendered only `<h2>` titles. `EventCard` and `TicketCounter` were empty stubs. There was no state — no way for a user to select tickets, track a cart, or filter events.

### After

All interactive state lives in `EventAppState`, managed by `useReducer`. An `EventProvider` wraps the app and exposes state and `dispatch` to every component. `TicketCounter` dispatches `ADD_TICKET` and `REMOVE_TICKET` actions directly from context. A `CartSummary` component shows the total tickets selected and allows the user to clear their cart.

---

## New File Structure

```
src/
  types/
    eventActions.ts          ← state shape + action type definitions
  reducers/
    eventReducer.ts          ← pure reducer function
  contexts/
    EventContext.tsx          ← Provider component + EventContext export
    useEventContext.ts        ← custom hook (separated for Vite Fast Refresh)
  components/
    EventCard.tsx             ← full event card with badges + TicketCounter
    TicketCounter.tsx         ← − / + ticket selection (reads context directly)
    EventList.tsx             ← reads filteredEvents from context (no props)
```

> **Note on `useEventContext.ts`:** The custom hook lives in its own file (separate from `EventContext.tsx`) to satisfy Vite's Fast Refresh requirement that a file export only React components or only non-component functions — not both.

---

## `useReducer`

### What it is

`useReducer` is a React hook that manages state through a **reducer function** — a pure function that takes the current state and an action, and returns the next state. It replaces multiple `useState` calls with a single structured state object, and replaces scattered handler functions with a centralized switch statement.

```tsx
const [state, dispatch] = useReducer(eventReducer, initialState);
```

- `state` — the current `EventAppState` object
- `dispatch` — a function used to send actions to the reducer
- `eventReducer` — the function that defines how state changes in response to actions
- `initialState` — the starting value of state

### The State Shape

Defined in `src/types/eventActions.ts`:

```ts
export type EventAppState = {
  events: Event[]; // full event list (from data/events.ts)
  filter: string; // active text filter ("All" or a search string)
  ticketCounts: Record<number, number>; // eventId → number of tickets selected
  cartTotal: number; // total tickets selected across all events
};
```

`ticketCounts` is a **dictionary keyed by event ID**. This means adding or removing a ticket for one event is a single object spread — no array mapping required. `cartTotal` is maintained as a running total so the `CartSummary` component can read a single number without summing the dictionary.

### The Action Union

Also in `src/types/eventActions.ts`:

```ts
export type EventAction =
  | { type: "ADD_TICKET"; eventId: number }
  | { type: "REMOVE_TICKET"; eventId: number }
  | { type: "SET_FILTER"; filter: string }
  | { type: "CLEAR_CART" };
```

This is a **discriminated union** — every possible state transition is named and typed. TypeScript enforces correct payloads at the call site. The four actions cover the complete set of things a user can do in this UI.

### The Reducer

Defined in `src/reducers/eventReducer.ts`:

```ts
export function eventReducer(
  state: EventAppState,
  action: EventAction,
): EventAppState {
  switch (action.type) {
    case "ADD_TICKET": {
      const current = state.ticketCounts[action.eventId] ?? 0;
      const event = state.events.find((e) => e.id === action.eventId);
      // Guard: cannot exceed availableTickets
      if (!event || current >= event.availableTickets) return state;
      return {
        ...state,
        ticketCounts: { ...state.ticketCounts, [action.eventId]: current + 1 },
        cartTotal: state.cartTotal + 1,
      };
    }

    case "REMOVE_TICKET": {
      const current = state.ticketCounts[action.eventId] ?? 0;
      // Guard: cannot go below 0
      if (current <= 0) return state;
      return {
        ...state,
        ticketCounts: { ...state.ticketCounts, [action.eventId]: current - 1 },
        cartTotal: state.cartTotal - 1,
      };
    }

    case "SET_FILTER":
      return { ...state, filter: action.filter };

    case "CLEAR_CART":
      return { ...state, ticketCounts: {}, cartTotal: 0 };
  }
}
```

Key properties of this reducer:

- **Pure function** — no side effects; given the same inputs, always returns the same output.
- **Immutable updates** — every case returns a new state object. The spread operator (`...state`) ensures the original is never mutated.
- **Business rules live here** — the cap at `availableTickets` and the floor at `0` are enforced in the reducer, not in the component. No matter how `dispatch` is called, the rules cannot be bypassed.
- **Atomic transitions** — `ADD_TICKET` updates both `ticketCounts` and `cartTotal` in a single return. They are always in sync.
- **No `default` case** — TypeScript's exhaustive checking catches any unhandled action at compile time.

---

## Context API

### What it is

The React Context API provides a way to pass data through the component tree without manually passing props at every level. It is the mechanism used to make `state` and `dispatch` available to components like `TicketCounter` that are several levels deep in the tree — without every intermediate component needing to pass them down.

### The Provider

Defined in `src/contexts/EventContext.tsx`:

```tsx
export function EventProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(eventReducer, initialState);

  // Derived value — computed from state, not stored separately
  const filteredEvents =
    state.filter === "All"
      ? state.events
      : state.events.filter((e) =>
          e.title.toLowerCase().includes(state.filter.toLowerCase()),
        );

  return (
    <EventContext.Provider value={{ state, dispatch, filteredEvents }}>
      {children}
    </EventContext.Provider>
  );
}
```

`EventProvider` owns the `useReducer` instance and computes `filteredEvents` as a derived value — it is recalculated on every render from `state.events` and `state.filter`, so it is never stale and never stored separately.

### The Custom Hook

Defined in `src/contexts/useEventContext.ts` (its own file):

```ts
import { useContext } from "react";
import { EventContext } from "./EventContext";

export function useEventContext() {
  const context = useContext(EventContext);
  if (!context) {
    throw new Error("useEventContext must be used within an EventProvider");
  }
  return context;
}
```

`useEventContext()` is the consumer API. Any component imports and calls it to access `state`, `dispatch`, or `filteredEvents`. The guard clause produces a clear, actionable error if a component renders outside the provider tree.

The hook lives in a **separate file** from the provider. This is required by Vite's Fast Refresh feature: a file must export only React components or only non-component functions. `EventContext.tsx` exports `EventProvider` (a component) and `EventContext` (a context object). Moving `useEventContext` into its own `.ts` file keeps both files Fast Refresh-compatible.

### How Components Use It

**`App.tsx`** — wraps the tree in the provider; `CartSummary` reads context directly:

```tsx
function App() {
  return (
    <EventProvider>
      <div className="app">
        <h1>OSU Event Finder</h1>
        <CartSummary /> {/* reads cartTotal, dispatches CLEAR_CART */}
        <EventList /> {/* reads filteredEvents */}
      </div>
    </EventProvider>
  );
}
```

**`EventList.tsx`** — no longer accepts an `events` prop:

```tsx
export function EventList() {
  const { filteredEvents } = useEventContext();
  // renders an EventCard for each event
}
```

**`TicketCounter.tsx`** — reads ticket count and dispatches without receiving any handlers:

```tsx
export function TicketCounter({
  eventId,
  availableTickets,
}: TicketCounterProps) {
  const { state, dispatch } = useEventContext();
  const count = state.ticketCounts[eventId] ?? 0;

  return (
    <div className="ticket-counter">
      <button
        onClick={() => dispatch({ type: "REMOVE_TICKET", eventId })}
        disabled={count === 0}
      >
        −
      </button>
      <span>{count}</span>
      <button
        onClick={() => dispatch({ type: "ADD_TICKET", eventId })}
        disabled={count >= availableTickets}
      >
        +
      </button>
    </div>
  );
}
```

---

## Component Tree: Before vs. After

### Before — static, no state

```
App (imports hardcoded events array)
  └── EventList (events prop — renders only <h2> titles)
        └── [EventCard — empty stub]
              └── [TicketCounter — returns null]
```

### After — context-driven, interactive

```
EventProvider (owns state + dispatch)
  └── App
        ├── CartSummary (reads cartTotal, dispatches CLEAR_CART)
        └── EventList (reads filteredEvents from context)
              └── EventCard (event prop only — for display data)
                    └── TicketCounter (reads ticketCounts, dispatches ADD/REMOVE)
```

`EventList` and `EventCard` never touch `ticketCounts` or `dispatch` — they are not in the data path for those concerns. `TicketCounter` reaches into context itself, exactly where it needs to.

---

## Why It Matters

| Concern                        | Before                                    | After                                                                  |
| ------------------------------ | ----------------------------------------- | ---------------------------------------------------------------------- |
| **Interactivity**              | None — static render of hardcoded data    | Full ticket selection with cart tracking                               |
| **State location**             | No state                                  | 1 `useReducer` in `EventProvider`                                      |
| **Business rules**             | N/A                                       | Enforced in the reducer (capacity cap, floor at 0)                     |
| **Atomic updates**             | N/A                                       | `ticketCounts` and `cartTotal` always updated together                 |
| **Prop drilling**              | `events` passed top-to-bottom             | Eliminated — leaf components read context directly                     |
| **Component responsibilities** | `EventList` rendering data it doesn't use | Each component only touches what it needs                              |
| **Testability**                | Nothing to test                           | `eventReducer` is a pure function — trivial to unit test without React |
| **TypeScript safety**          | N/A                                       | `dispatch` only accepts the defined `EventAction` union                |
