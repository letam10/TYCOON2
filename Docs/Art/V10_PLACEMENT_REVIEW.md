# V10 world prop placement review

## Evidence and method

Reviewed genuine Windows Player PNGs under
`work/items-vehicles-20261009/player-20261009T124757000Z`.
The contact sheets under `work/v10-placement-review` are labeled by owner.
For each prop and diner table, I inspected all nine isolated angles
(`front`, `front-left`, `left`, `rear-quarter`, `rear`, `rear-right`,
`right`, `three-quarter`, `top`) and all five placed angles
(`front`, `left`, `rear`, `right`, `top`).

### Prop and furniture owners

- Safe: `safe_starter`.
- Storage: `storage_bakery`, `storage_farm`, `storage_farm_shop`,
  `storage_processing`, `storage_restaurant`, `storage_supermarket`.
- Shelves: `shelf_bakery`, `shelf_farm_1`, `shelf_farm_2`,
  `shelf_livestock`, `shelf_market_1`, `shelf_market_2`, `shelf_market_3`.
- Checkout: `checkout_bakery`, `checkout_farm`, `checkout_farm_shop`,
  `checkout_market`, `checkout_market_2`, `checkout_restaurant`.
- Diner tables: `table_0`, `table_1`, `table_2`, `table_3`, `table_4`,
  `table_5`.

Isolated files use `world-single-<owner>-<angle>.png`; placed files use
`world-model-<owner>-<angle>.png`. Contact sheets `isolated-01.jpg` through
`isolated-07.jpg` and `placed-01.jpg` through `placed-07.jpg` cover these
owners in the listed order, four owners per sheet where possible.

### Static landmarks

Inspected five placed angles (`front`, `left`, `rear`, `right`, `top`) for
`EntertainmentHall`, `FireStation`, `FoundersMonument`, `Playground`,
`PoliceStation`, and `SeasideBar`, using
`city-placed-<landmark>-<angle>.png`. Sheets `landmarks-01.jpg` and
`landmarks-02.jpg` show all six. Beach and Harbor water are excluded.

## Findings

- No definite broken or missing geometry was visible in the reviewed angles.
  The safe body, door hardware and two ground discs read as assembled; the
  storage racks and shelf tables keep a consistent silhouette across views.
- The six storage variants share a very similar rack silhouette, and the
  seven shelf variants share a similar display table. The screenshots do not
  establish whether that repetition is intentional, so this is a design
  question rather than a confirmed defect.
- Checkout counters and all six diner table-and-seat sets appear assembled
  in their isolated views. In placed views, neighboring characters, walls,
  counters and other furniture obscure portions of some objects at particular
  camera angles. Those views do not show enough to call the hidden portions
  broken geometry.
- The six static landmarks are visible on paved footprints in the placed
  views. Their forms are simple low-poly buildings or site props; no definite
  intersection or detached part is evident in the five angles reviewed.

## Limits

This is a visual review of the supplied Player captures. Occlusion and framing
can hide geometry. The captures do not establish collision shapes, exact world
bounds, interaction behavior, or placement validity in a live session.
Goods, vehicles, industrial machines, and Beach/Harbor water are outside this
review.
