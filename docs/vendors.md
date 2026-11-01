# Vendors

Each vendor is one shop for everyone: its stock, its own money and its weekly budget follow
every trade. Each player's own money stays their own.

## How it works

- A vendor's state lives in `GameSave.vendorSystem` (world state), but a trade changed it only on
  the trader's machine.
- After a deal (`Trading.DoAcceptDeal`), the trading machine sends that vendor's whole state
  (`Vendor`, the game's `LazySerializer`); the host passes it on.
- Every few seconds the host also sends any vendor that changed on its own (daily restock, its
  own trades).
- A receiver loads the state into its copy, except while its own player has that vendor's trade
  window open: a pending basket has already been taken out of the stock, so replacing the stock
  then could duplicate or lose items. That update waits until the window closes.

## Files

[CoopVendorSync.cs](../src/GK2Coop/CoopVendorSync.cs).

## Messages

`GK2Coop.Vendor.v1` (any → host → others).

## Settings

`[Coop] ShareVendors` (true).

## Limits

Only one player at a time should trade with a vendor. If two trade with the same vendor at the
same time, both can get the vendor's last item (README, "Limits of this version").

## Tests

Regression run `vendor` (Full profile).
