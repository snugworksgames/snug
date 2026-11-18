# Snug: launch plan

Goal: get Snug onto phones through a store, with light monetization, at the lowest cost and risk.

## Recommendation in one paragraph

Ship the **web game inside a native app shell (Capacitor), on Google Play first**. The web game is the real product now: it has the art, the variety and the playtested feel, and it already has hooks for native ads and haptics. The Unity prototype is far behind it and would take weeks to catch up with no evidence it would play better. Add the App Store only once Android shows the game holds players, because Apple costs $99 every year and needs a Mac.

## What each store costs and requires

| | Google Play | Apple App Store |
|---|---|---|
| Fee | $25 once | $99 per year |
| Needs | Any computer | A Mac with Xcode |
| Before going public | **Closed test with at least 12 testers opted in for 14 days in a row** (rule for personal accounts created after 13 Nov 2023; checked against Google's Play Console help page, Nov 2026) | TestFlight optional, App Review required |
| Developer name shown | The name you choose (Snugworks) | Your legal name, unless you register a company |

The 12 testers can be your friends and family. Each needs an Android phone and a Google account, and has to stay opted in for the 14 days.

## Steps

**You (accounts only you can create):**
1. Google Play developer account (personal, $25, identity check). Start now: verification can take days.
2. AdMob account, linked to the app once it exists.
3. Collect 12+ testers' Google email addresses.

**Me (in this repo):**
1. Capacitor project that wraps `docs/` into an Android app: bundled offline, portrait, proper icon and splash.
2. Real ads through the existing `window.SNUG_ADS` hook (AdMob): the optional "keep going" video, optional 2x coins, and the capped between-runs ad. Child-directed settings on, non-personalized ads only.
3. Native haptics through the existing `window.SNUG_HAPTIC` hook.
4. A GitHub Actions build that produces the signed app bundle, so you don't need Android Studio.
5. Privacy policy page (hosted on the same GitHub Pages site) and the store listing text and screenshots.

**Then:** upload to closed testing, run the 14 days, apply for production, release.

## Because kids love it: the rules that apply

Kids being the audience changes what is allowed, and declaring the audience honestly matters. Stores remove apps that say "not for children" while clearly appealing to them.

- **Google Play Families policy:** if the target audience includes children, ads must come from a Families self-certified ad network (AdMob qualifies when configured for it), with child-directed treatment and no personalized ads. Ad content must be suitable for children.
- **India (DPDP Act and Rules):** anyone under 18 counts as a child. Verifiable parental consent is needed to process their personal data, and tracking or targeted advertising aimed at them is banned. The Rules were notified in November 2025 with a phased rollout; reporting I found puts the main obligations at about May 2027. Check the current state before launch.
- **What this means for Snug:** collect no personal data (it collects none today), use only non-personalized, child-suitable ads, and put a simple parental gate in front of any purchase or outside link.

## Money: an honest estimate

"A pizza every two weeks" is roughly ₹500 per two weeks, about ₹36 (US$0.40) a day.

- Kid-safe, non-personalized ads pay much less than normal ads, and Indian ad rates are low. My rough working assumption is ₹0.1 to ₹0.5 per daily player per day. That is an assumption, not a measurement.
- At that rate the pizza needs somewhere around **100 to 400 people playing every day**.
- AdMob only pays out once your balance passes its minimum threshold (about US$100; confirm on your AdMob payments page). At pizza pace the first payout could take most of a year.
- Costs: $25 once for Google Play. Apple's $99 a year would eat roughly half the pizza money, which is why it comes second.

**A better fit at small scale:** one cheap optional purchase, for example "Unlock all hats and skins" for ₹49 to ₹99, behind a parental gate. One purchase is worth thousands of ad views. I'd add it alongside the ads.

## What decides whether this works

Getting a few hundred daily players, not the technology. The cheapest levers, in order:
1. **Challenge links** (built): every shared score is an invitation. Watch whether kids actually send them.
2. **Day-2 return:** the daily reward, missions and zones exist for this. Ask your testers on day 3 whether they opened it again.
3. **A short gameplay video** for the store page and for sharing in class and family groups.

## Order of work

| Step | Who | Time |
|---|---|---|
| Play developer account + tester emails | You | Start now, a few days |
| Capacitor app, ads, haptics, CI build, privacy policy | Me | One or two sessions |
| Closed test | You + 12 testers | 14 days minimum |
| Fixes from the test, store listing | Both | During the 14 days |
| Production release on Google Play | You | After approval |
| App Store | Later | Only if Android numbers justify $99 a year |

## Costs: what can and can't charge you

Budget: the one-time $25 Google Play fee and nothing else.

**Snug itself can't run up a bill.** It has no server, no database and no API keys, and it calls no paid service. The game
runs entirely on the player's phone; the website is free GitHub Pages; ads *pay you* (AdMob never charges). Every build runs
`tools/check-secrets.mjs`, which fails if anything that looks like an API key, token or signing key is committed.

**Account switches to keep it at $0:**
- **GitHub (snugworksgames organization):** Settings → Billing and plans. Keep no payment method on file and every spending
  limit at $0 (the default without a card). Then Actions, Packages and Codespaces simply stop at the free allowance instead of
  charging. Builds of this public repository use free minutes anyway.
- **Google Play Console:** the $25 registration is the only charge. There are no per-download or hosting fees.
- **AdMob:** only ever pays out. The payment details it asks for are where *you* receive money, not a card it charges.
- **Google Cloud:** don't create a Cloud billing account; nothing in Snug needs one. (Cloud "budgets" only send alerts, they
  don't stop spending, so the safe cap is having no billing account at all.)
- **Never add a paid API** (AI, maps, analytics with paid tiers) to the game. A key inside an app or web page can be copied by
  anyone, which is exactly how the large surprise bills happen.
- **Apple:** skipped for now ($99 a year).

## Store setup (Snug Plus and Remove Ads)

The app's code for both purchases is built in (build 15) and tested with a simulated Google Play (`?iap=sim`). To make them
real, once the Play Console account exists:

1. Play Console → your app → **Monetize → Products → In-app products** → create two one-time ("managed") products:
   - `snug_plus` (Snug Plus: no ads between runs, every skin and hat in the shop, the Golden blob)
   - `snug_remove_ads` (Remove Ads)
2. Set prices per country by hand rather than auto-converting: India ₹129 / ₹79, US $2.99 / $1.99, and similar elsewhere.
3. Activate both. Purchases can be tested by adding testers as **license testers** (Setup → License testing): they buy
   with test cards and are never charged.
4. In the **Data safety** form, purchases count as "purchase history", handled by Google Play; Snug collects nothing else.

Fair-play rules the code follows: cosmetics and comfort only, a parental gate before every purchase, no offers during a run
or in the first session, at most one contextual offer a day, and Google Play decides what you own (refunds take perks away).

