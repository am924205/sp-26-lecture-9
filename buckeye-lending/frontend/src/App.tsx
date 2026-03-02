import { LoanProvider } from "./contexts/LoanContext";
import Dashboard from "./components/Dashboard";
import "./App.css";

// ─── App ─────────────────────────────────────────────────────────────────────
// LoanProvider owns the reducer + fetch side-effect.
// All descendants access state via useLoanContext().
function App() {
  return (
    <LoanProvider>
      <Dashboard />
    </LoanProvider>
  );
}

export default App;
