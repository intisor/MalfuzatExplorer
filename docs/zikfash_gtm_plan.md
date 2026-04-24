# ZikFash — Go-To-Market & Product Strategy

## The Core Idea
ZikFash is a **tailor's command centre in a browser tab**.  
It ships as a public URL but behaves differently depending on who's using it.

---

## 1. User Tiers

### 🧵 Onboarded Tailor (via landing page invite link)
The real product. They arrive via a link like:

```
zikfash.app/?ref=tailor&shop=amaka-fashion&region=ng&currency=NGN
```

They get:
- Full customer database (Blazored.LocalStorage — device-persisted)
- Online backup & restore (encrypted JSON export / cloud sync future)
- Snap-a-Record OCR feature (future — photo → auto-fill measurements)
- Dashboard KPIs (customers, pending orders, revenue)
- Currency & region set from their `?currency=NGN` param at onboarding
- Their shop name personalized in the UI header
- WhatsApp share with their branding
- Unlimited measurements per customer

### 👀 Guest / Public Visitor (arrives from landing page or cold traffic)
A **taste** of the product. No storage, no account.

They get:
- A demo tailor mode — can add 1–2 dummy customers with measurements
- The same beautiful tape-measure UI
- At the measurement save step → soft paywall CTA:  
  *"Save to your profile — onboard as a tailor in 30 seconds"*
- Can view a read-only shared customer card (if a tailor shares a link)

> **Key rule:** Guests never see raw data from a real tailor's shop.  
> Sessions are ephemeral — nothing persists past tab close.

---

## 2. Onboarding Flow (Landing Page)

```
Landing Page (zikfash.app)
    ↓
Hero: "Your tailor shop. In your pocket."
    ↓
[Try as Guest]          [I'm a Tailor — Get Started Free]
    ↓                           ↓
Guest demo mode         Short form:
(ephemeral)             - Shop name
                        - Country / Currency
                        - WhatsApp number (optional)
                        - [Generate My Link]
                            ↓
                        zikfash.app/app?ref=tailor&shop=...&region=...&currency=...
                        (saved to localStorage on first load)
```

The generated link is their **permanent bookmark** — they save it to their home screen as a PWA. Query params are read once, stored in `localStorage`, and never needed in the URL again.

---

## 3. Monetization

### Phase 1 — Free & Viral (Launch)
- 100% free for all tailors
- Viral mechanic: every WhatsApp share has a subtle  
  *"Powered by ZikFash — zikfash.app"* footer
- Goal: get 500 real tailors using it

### Phase 2 — ZikFash Pro (3–6 months post-launch)

| Feature | Free | Pro (≈ $3–5/mo) |
|---|---|---|
| Customers | Up to 30 | Unlimited |
| Measurements per customer | All standard | + Custom fields |
| Backup & restore | Manual export | Auto cloud backup |
| Snap-a-Record OCR | ❌ | ✅ |
| WhatsApp share | With ZikFash branding | Custom shop branding |
| Multi-device sync | ❌ | ✅ |
| Revenue/order tracking | ❌ | ✅ |
| Offline-first PWA | ✅ | ✅ |

> Pro pricing in local currency — $3 in 🇺🇸, ₦4,500 in 🇳🇬, £2.50 in 🇬🇧 etc.  
> Handled via Stripe + Paystack (Nigeria) dual gateway.

### Phase 3 — Marketplace (Future)
- Customers can **request** a tailor via ZikFash (lead gen revenue)
- Tailors pay per verified lead or subscription tier

---

## 4. Global Appeal Strategy

### The Problem with "Nigerian apps"
They feel local. Currency is hardcoded (`₦`). Language assumes context.  
ZikFash must feel like it was built **for every tailor on earth**.

### The Fix

**From the landing page query param:**
```
?region=gh&currency=GHS   → Ghana Cedis, "Hello from Accra"
?region=uk&currency=GBP   → British Pounds, "Hello from London"
?region=us&currency=USD   → US Dollars
```

**In the dashboard:**
- All monetary values use the stored currency symbol
- Date formats adapt to locale
- Language: English by default, French/Arabic/Yoruba roadmap

**Tone:**
- Not "tailor shop" (sounds Nigerian)
- "Fashion studio" / "Atelier" in global copy
- Body measurement system: **inches** (default) OR **cm** — toggle at onboarding

**Marketing positioning:**
> *"The measurement app for custom fashion professionals — from Lagos to London."*

---

## 5. Snap-a-Record (Future Feature — Plan Only)

User takes a photo of a handwritten or printed measurement card.

**Flow:**
1. Tailor taps **"Snap Record"** button
2. Camera opens → photo of paper record taken
3. Image sent to Gemini Vision API (or on-device ML)
4. Gemini extracts: customer name, measurement labels + values
5. ZikFash pre-fills a new customer profile
6. Tailor reviews and confirms — one tap to save

**Why this matters:**
- Every tailor has a box of old paper records
- First-time onboarding: they can migrate years of customers in minutes
- This is the **killer feature** for Pro conversion

---

## 6. Storage Architecture (Guest vs Tailor)

```
Guest Session
└── sessionStorage only (cleared on tab close)
    ├── temp_customers[]
    └── demo_mode: true

Onboarded Tailor
└── Blazored.LocalStorage (persistent, device-bound)
    ├── shop_config { name, currency, region, whatsapp }
    ├── customers[]
    ├── measurements{}
    └── last_backup_date

Pro Tailor (future)
└── LocalStorage (offline cache) + Cloud Sync
    ├── Supabase / Firebase as backend
    └── Conflict resolution: last-write-wins per customer
```

---

## 7. Immediate Next Steps (Before Launch)

- [ ] Build the landing page with the two CTAs and onboarding form
- [ ] Implement query-param reader on app load → persist to localStorage
- [ ] Add guest mode detection (no localStorage write for guests)
- [ ] Add currency symbol to dashboard from stored `shop_config`
- [ ] Soft paywall modal for guests who try to save
- [ ] PWA manifest + "Add to Home Screen" prompt
- [ ] Snap-a-Record design mockup (not coded yet)
