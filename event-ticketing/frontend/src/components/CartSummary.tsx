import { useEventContext } from "../contexts/useEventContext";

// ─── CartSummary ──────────────────────────────────────────────────────────────
// Reads cartTotal from context and dispatches CLEAR_CART.
// Only renders when tickets have been selected.
export default function CartSummary() {
  const { state, dispatch } = useEventContext();
  if (state.cartTotal === 0) return null;

  return (
    <div className="cart-summary">
      <span>
        🎟 {state.cartTotal} ticket{state.cartTotal !== 1 ? "s" : ""} selected
      </span>
      <button onClick={() => dispatch({ type: "CLEAR_CART" })}>
        Clear Cart
      </button>
    </div>
  );
}
