import { useLoanContext } from "../contexts/LoanContext";
import LoanApplicationList from "./LoanApplicationList";

// ─── Dashboard ───────────────────────────────────────────────────────────────
// Reads state and dispatch directly from context — no useState, no prop drilling.
export default function Dashboard() {
  const { state, dispatch, filteredLoans, loanTypes } = useLoanContext();

  return (
    <div className="app">
      <header>
        <h1>Buckeye Lending</h1>
        <p>Loan Application Dashboard</p>
        {state.notificationCount > 0 && (
          <button
            className="notification-badge"
            onClick={() => dispatch({ type: "CLEAR_NOTIFICATIONS" })}
          >
            {state.notificationCount} notification
            {state.notificationCount !== 1 ? "s" : ""} — Clear
          </button>
        )}
      </header>
      <main>
        {state.loading && <p className="loading">Loading loan applications…</p>}
        {state.error && (
          <p className="error">Failed to load applications: {state.error}</p>
        )}
        {!state.loading && !state.error && (
          <>
            <div className="type-filter">
              {loanTypes.map((type) => (
                <button
                  key={type}
                  className={type === state.filter ? "active" : ""}
                  onClick={() => dispatch({ type: "SET_FILTER", filter: type })}
                  type="button"
                >
                  {type}
                </button>
              ))}
            </div>
            <LoanApplicationList />
            <p className="loan-count">{filteredLoans.length} applications</p>
          </>
        )}
      </main>
    </div>
  );
}
