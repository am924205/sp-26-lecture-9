import { EventProvider } from "./contexts/EventContext";
import CartSummary from "./components/CartSummary";
import { EventList } from "./components/EventList";
import "./App.css";

// ─── App ─────────────────────────────────────────────────────────────────────
// EventProvider owns the reducer. All descendants access state via useEventContext().
function App() {
  return (
    <EventProvider>
      <div className="app">
        <h1>OSU Event Finder</h1>
        <CartSummary />
        <EventList />
      </div>
    </EventProvider>
  );
}

export default App;
