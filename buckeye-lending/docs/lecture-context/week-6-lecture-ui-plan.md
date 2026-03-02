# LIVE DEMO — Loan Dashboard\*\*

**Slide Title:** Let's Build the Loan Dashboard

**Content:**

- Starting from a fresh React + TypeScript project
- Building LoanApplicationCard as a presentational component
- Building LoanApplicationList using `.map()`
- Adding conditional rendering for status badges and risk indicators
- Applying CSS Modules for clean styling

**[INSTRUCTOR: Switch to VS Code]**

**Speaker Notes:** "Let's build. I'm going to create a loan officer dashboard for Buckeye Lending. Watch how I think about component design as I go. The patterns you see here — `.map()`, conditional rendering, CSS Modules — are the same patterns you'll use for your Marketplace product catalog."

**[See LIVE CODING SCRIPT below]**

**Timing:** 15 minutes

---

## **LIVE CODING SCRIPT (15 minutes)**

### Pre-built: Loan application data file (show briefly — 1 min)

"I have a loan applications data file already set up. This extends our Week 5 data with more loans and richer fields."

**File: `src/data/loanApplications.ts`** (already exists — show it)

```tsx
export type LoanApplication = {
  id: number;
  applicantName: string;
  loanAmount: number;
  loanType: string;
  annualIncome: number;
  status: string;
  riskRating: number;
  submittedDate: string;
  notes: string;
};

export const loanApplications: LoanApplication[] = [
  {
    id: 1,
    applicantName: "Sarah Johnson",
    loanAmount: 250000,
    loanType: "Mortgage",
    annualIncome: 95000,
    status: "Approved",
    riskRating: 2,
    submittedDate: "2026-01-15",
    notes: "Strong credit history, stable employment for 8 years",
  },
  {
    id: 2,
    applicantName: "Michael Chen",
    loanAmount: 32500,
    loanType: "Auto",
    annualIncome: 68000,
    status: "Pending",
    riskRating: 3,
    submittedDate: "2026-02-01",
    notes: "First-time auto loan, good income-to-debt ratio",
  },
  {
    id: 3,
    applicantName: "Emily Rodriguez",
    loanAmount: 320000,
    loanType: "Mortgage",
    annualIncome: 72000,
    status: "Denied",
    riskRating: 5,
    submittedDate: "2026-01-28",
    notes: "Income-to-loan ratio exceeds threshold, recent credit issues",
  },
  {
    id: 4,
    applicantName: "David Kim",
    loanAmount: 15000,
    loanType: "Personal",
    annualIncome: 52000,
    status: "Approved",
    riskRating: 2,
    submittedDate: "2026-02-03",
    notes: "Consolidating credit card debt, excellent payment history",
  },
  {
    id: 5,
    applicantName: "Jessica Martinez",
    loanAmount: 500000,
    loanType: "Business",
    annualIncome: 150000,
    status: "Under Review",
    riskRating: 4,
    submittedDate: "2026-02-05",
    notes: "New restaurant venture, limited business credit history",
  },
  {
    id: 6,
    applicantName: "James Wilson",
    loanAmount: 28000,
    loanType: "Auto",
    annualIncome: 75000,
    status: "Approved",
    riskRating: 1,
    submittedDate: "2026-01-20",
    notes: "Repeat customer, excellent credit score 780+",
  },
  {
    id: 7,
    applicantName: "Amanda Foster",
    loanAmount: 175000,
    loanType: "Mortgage",
    annualIncome: 88000,
    status: "Pending",
    riskRating: 3,
    submittedDate: "2026-02-10",
    notes: "First-time homebuyer, pending employment verification",
  },
  {
    id: 8,
    applicantName: "Robert Taylor",
    loanAmount: 75000,
    loanType: "Business",
    annualIncome: 120000,
    status: "Denied",
    riskRating: 4,
    submittedDate: "2026-02-08",
    notes: "Insufficient collateral for requested amount",
  },
];
```

**Narrate:** "Eight loan applications across four types — Mortgage, Auto, Personal, Business. Mixed statuses: approved, pending, denied, under review. Risk ratings from 1 (low) to 5 (high). This gives us plenty to work with for conditional rendering.

You'll notice Sarah, Michael, and Emily are back from Week 5 — we've just added five more applicants and richer data."

### Step 1: Build LoanApplicationCard Component (4 min)

**Create file: `src/components/LoanApplicationCard.tsx`**

```tsx
import type { LoanApplication } from "../data/loanApplications";

type LoanApplicationCardProps = {
  loan: LoanApplication;
};

function LoanApplicationCard({ loan }: LoanApplicationCardProps) {
  return (
    <div className="loan-card">
      <div className="loan-header">
        <span className="loan-type">{loan.loanType}</span>
        <span className="loan-status">{loan.status}</span>
      </div>
      <div className="loan-info">
        <h3 className="loan-applicant">{loan.applicantName}</h3>
        <p className="loan-amount">${loan.loanAmount.toLocaleString()}</p>
        <p className="loan-income">
          Income: ${loan.annualIncome.toLocaleString()}/yr
        </p>
        <p className="loan-date">
          Submitted: {new Date(loan.submittedDate).toLocaleDateString()}
        </p>
        <button className="details-btn">View Details</button>
      </div>
    </div>
  );
}

export default LoanApplicationCard;
```

**Narrate:** "Here's the LoanApplicationCard component. It takes a single prop: a `loan` object. The type comes from our data file — TypeScript makes sure we're using the right fields.

Notice the structure: a header section with loan type and status, then an info section with applicant name, amount, income, and date. This is a presentational component — it receives data and displays it. No state. No logic. Just rendering.

The amount uses `.toLocaleString()` to add comma formatting — $250,000 instead of 250000. Small detail, but it matters for readability.

This is the same pattern you'd use for a ProductCard — different fields, same structure."

### Step 2: Build LoanApplicationList with .map() (3 min)

**Create file: `src/components/LoanApplicationList.tsx`**

```tsx
import type { LoanApplication } from "../data/loanApplications";
import LoanApplicationCard from "./LoanApplicationCard";

type LoanApplicationListProps = {
  loans: LoanApplication[];
};

function LoanApplicationList({ loans }: LoanApplicationListProps) {
  return (
    <div className="loan-grid">
      {loans.map((loan) => (
        <LoanApplicationCard key={loan.id} loan={loan} />
      ))}
    </div>
  );
}

export default LoanApplicationList;
```

**Narrate:** "LoanApplicationList takes an array of loans and renders a LoanApplicationCard for each one. That's the `.map()` pattern in action.

Notice three things. First, `key={loan.id}` — every loan has a unique ID, so React can track each card. Second, the entire loan object is passed as a prop — LoanApplicationCard decides what to display. Third, LoanApplicationList doesn't know or care how many loans there are — it renders however many you give it.

This component is generic. If you renamed it to `ItemList` and the card to `ItemCard`, it works for any data — loans, products, orders, anything.

Let me wire this into App and see it in the browser..."

**Update `src/App.tsx`:**

```tsx
import { loanApplications } from "./data/loanApplications";
import LoanApplicationList from "./components/LoanApplicationList";

function App() {
  return (
    <div className="app">
      <header>
        <h1>Buckeye Lending</h1>
        <p>Loan Application Dashboard</p>
      </header>
      <main>
        <LoanApplicationList loans={loanApplications} />
      </main>
    </div>
  );
}

export default App;
```

**[Switch to browser, show the loan list rendering]**

**Narrate:** "Eight loan application cards, all rendered from the array. If I add a ninth loan to the data file, a ninth card appears automatically. Remove one, it disappears. That's the power of data-driven rendering — the UI reflects the data."

### Step 3: Add Conditional Rendering (4 min)

**Update `src/components/LoanApplicationCard.tsx`:**

```tsx
import type { LoanApplication } from "../data/loanApplications";

type LoanApplicationCardProps = {
  loan: LoanApplication;
};

function getStatusColor(status: string): string {
  switch (status) {
    case "Approved":
      return "status-approved";
    case "Denied":
      return "status-denied";
    case "Pending":
      return "status-pending";
    case "Under Review":
      return "status-review";
    default:
      return "";
  }
}

function LoanApplicationCard({ loan }: LoanApplicationCardProps) {
  return (
    <div className={`loan-card ${loan.status === "Denied" ? "denied" : ""}`}>
      <div className="loan-header">
        <span className="loan-type">{loan.loanType}</span>
        <span className={`loan-status ${getStatusColor(loan.status)}`}>
          {loan.status}
        </span>
      </div>
      <div className="loan-info">
        <h3 className="loan-applicant">{loan.applicantName}</h3>
        <p className="loan-amount">${loan.loanAmount.toLocaleString()}</p>
        <p className="loan-income">
          Income: ${loan.annualIncome.toLocaleString()}/yr
        </p>
        {loan.riskRating >= 4 && (
          <span className="high-risk-badge">⚠ High Risk</span>
        )}
        {loan.loanAmount > 200000 && (
          <span className="large-loan-badge">Large Loan</span>
        )}
        <p className="loan-date">
          Submitted: {new Date(loan.submittedDate).toLocaleDateString()}
        </p>
        <button className="details-btn" disabled={loan.status === "Denied"}>
          {loan.status === "Denied" ? "Application Closed" : "View Details"}
        </button>
      </div>
    </div>
  );
}

export default LoanApplicationCard;
```

**Narrate:** "Now we add conditional rendering. Four examples:

First — the status badge colors. A helper function maps each status to a CSS class. Approved gets green, Denied gets red, Pending gets yellow, Under Review gets blue. Same data, different visual treatment.

Second — the 'High Risk' warning badge. `{loan.riskRating >= 4 && <span>⚠ High Risk</span>}`. The `&&` operator says: if the left side is true, render the right side. If false, render nothing. Emily and Jessica have risk ratings of 4+, so they get the warning.

Third — the 'Large Loan' badge. `{loan.loanAmount > 200000 && <span>Large Loan</span>}`. Loans over $200K get flagged.

Fourth — the button uses a ternary: `loan.status === 'Denied' ? 'Application Closed' : 'View Details'`. Denied applications get a disabled button.

These are the two conditional rendering patterns you'll use everywhere:

- `{condition && <element>}` — show or hide
- `{condition ? <elementA> : <elementB>}` — show one or the other

For your Marketplace, you might use these for 'In Stock' vs 'Sold Out', or sale badges, or rating stars. Same patterns, different conditions."

**[Show in browser — point out the colored status badges, high-risk warnings, disabled buttons]**

### Step 4: Add CSS Modules (3 min)

**Create file: `src/components/LoanApplicationCard.module.css`** (have this pre-written, just import it)

```css
.card {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
  transition: box-shadow 0.2s ease;
  display: flex;
  flex-direction: column;
}

.card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.denied {
  opacity: 0.6;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  background-color: #f8f8f8;
  border-bottom: 1px solid #e0e0e0;
}

.loanType {
  font-size: 0.75rem;
  color: #bb0000;
  text-transform: uppercase;
  font-weight: 600;
  letter-spacing: 0.5px;
}

.status {
  font-size: 0.75rem;
  padding: 2px 10px;
  border-radius: 12px;
  font-weight: 600;
}

.statusApproved {
  background-color: #e8f5e9;
  color: #2e7d32;
}

.statusDenied {
  background-color: #ffebee;
  color: #c62828;
}

.statusPending {
  background-color: #fff8e1;
  color: #f57f17;
}

.statusReview {
  background-color: #e3f2fd;
  color: #1565c0;
}

.info {
  padding: 16px;
  flex: 1;
  display: flex;
  flex-direction: column;
}

.applicant {
  font-size: 1.1rem;
  margin: 0 0 4px 0;
}

.amount {
  font-size: 1.25rem;
  font-weight: bold;
  color: #333;
  margin: 4px 0;
}

.income {
  font-size: 0.85rem;
  color: #666;
  margin: 2px 0;
}

.date {
  font-size: 0.8rem;
  color: #999;
  margin: 4px 0;
}

.highRiskBadge {
  display: inline-block;
  background-color: #ff5722;
  color: white;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 0.7rem;
  font-weight: bold;
  text-transform: uppercase;
  margin: 4px 0;
  width: fit-content;
}

.largeLoanBadge {
  display: inline-block;
  background-color: #9c27b0;
  color: white;
  padding: 2px 8px;
  border-radius: 4px;
  font-size: 0.7rem;
  font-weight: bold;
  text-transform: uppercase;
  margin: 4px 0;
  width: fit-content;
}

.button {
  width: 100%;
  padding: 10px;
  border: none;
  border-radius: 4px;
  font-size: 0.95rem;
  cursor: pointer;
  background-color: #bb0000;
  color: white;
  font-weight: 600;
  margin-top: auto;
}

.button:hover:not(:disabled) {
  background-color: #990000;
}

.button:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}
```

**Update LoanApplicationCard.tsx to use CSS Modules:**

```tsx
import type { LoanApplication } from "../data/loanApplications";
import styles from "./LoanApplicationCard.module.css";

type LoanApplicationCardProps = {
  loan: LoanApplication;
};

function getStatusClass(status: string): string {
  switch (status) {
    case "Approved":
      return styles.statusApproved;
    case "Denied":
      return styles.statusDenied;
    case "Pending":
      return styles.statusPending;
    case "Under Review":
      return styles.statusReview;
    default:
      return "";
  }
}

function LoanApplicationCard({ loan }: LoanApplicationCardProps) {
  return (
    <div
      className={`${styles.card} ${loan.status === "Denied" ? styles.denied : ""}`}
    >
      <div className={styles.header}>
        <span className={styles.loanType}>{loan.loanType}</span>
        <span className={`${styles.status} ${getStatusClass(loan.status)}`}>
          {loan.status}
        </span>
      </div>
      <div className={styles.info}>
        <h3 className={styles.applicant}>{loan.applicantName}</h3>
        <p className={styles.amount}>${loan.loanAmount.toLocaleString()}</p>
        <p className={styles.income}>
          Income: ${loan.annualIncome.toLocaleString()}/yr
        </p>
        {loan.riskRating >= 4 && (
          <span className={styles.highRiskBadge}>⚠ High Risk</span>
        )}
        {loan.loanAmount > 200000 && (
          <span className={styles.largeLoanBadge}>Large Loan</span>
        )}
        <p className={styles.date}>
          Submitted: {new Date(loan.submittedDate).toLocaleDateString()}
        </p>
        <button className={styles.button} disabled={loan.status === "Denied"}>
          {loan.status === "Denied" ? "Application Closed" : "View Details"}
        </button>
      </div>
    </div>
  );
}

export default LoanApplicationCard;
```

**Narrate:** "CSS Modules. Instead of a regular CSS file, I create `LoanApplicationCard.module.css`. Then I import it as `styles`. Instead of `className="card"`, I write `className={styles.card}`.

What's the difference? With regular CSS, if two components both define a `.card` class, they collide. One overrides the other. CSS Modules scopes the class names to this component. Under the hood, `.card` becomes something like `.LoanApplicationCard_card_x7f3q` — unique and collision-proof.

For your M3, I recommend CSS Modules. They're built into Vite — no extra setup. They keep your styles organized as your app grows."

**[Show in browser — styled loan dashboard with Scarlet and Gray theming, colored status badges]**

"Look at that — a professional-looking loan dashboard with status badges, risk indicators, and the OSU Scarlet color scheme. Denied applications are dimmed. High-risk loans have a warning badge. Hover effects on the cards. Clean, professional."

**BACKUP PLAN:** If live coding fails, have a working version in a separate branch. `git checkout week6-loan-dashboard-complete`

---
