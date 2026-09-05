# Reel Catch

Reel Catch is an original fishing-themed 5x3 slot added to the Casino project.

## Features

- 5 reels x 3 rows
- 10 fixed paylines
- Fisherman wild symbol
- 3+ shell scatters trigger 10 free spins
- During free spins, fish display cash values
- A fisherman collects all visible cash-fish values
- Cumulative fishermen upgrade the collector:
  - 4 fishermen: x2 + 5 free spins
  - 8 fishermen: x3 + 5 free spins
  - 12 fishermen: x10 + 5 free spins
- Server-side result generation and balance updates
- Existing `PlayerAccount` balance and `Spin` history are reused
- Bonus state is stored server-side in ASP.NET Core Session
- Responsive reel-stop, win, scatter and collection animations

## Files added

- `Services/ReelCatchEngine.cs`
- `Models/ReelCatchSpinResult.cs`
- `Views/Game/ReelCatch.cshtml`

## Files updated

- `Program.cs` - registers the engine and Session
- `Controllers/GameController.cs` - adds `ReelCatch` and `ReelCatchSpin`
- `Views/Home/Lobby.cshtml` - adds the Reel Catch lobby entry
- `wwwroot/css/site.css` - Reel Catch styling

No database migration is required for this game because it reuses the existing player and spin tables.

## Reel animation update
The reels now animate as five continuous vertical strips. They launch almost together, scroll vertically, decelerate, and stop from left to right with a short settling bounce. Decorative symbols shown while spinning do not affect the result; each reel lands on the board returned by the server.

## Underwater presentation update
The Reel Catch game now uses `wwwroot/images/reelcatch-undersea.png` as a softened underwater scene behind a borderless reel layout. Cabinet chrome, reel tiles and reel-window borders have been removed so the live symbols float directly over the scene. The generated spin result and bonus logic are unchanged.
