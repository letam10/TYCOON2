# V10 Goods Visual Review

Date: 2026-10-09  
Evidence: `work/items-vehicles-20261009/player-20261009T124757000Z`

## Coverage

Reviewed the supplied Windows Player captures for all 27 goods IDs:

`animal_feed`, `beef_soy`, `beef`, `bottled_milk`, `bread_dough`, `bread`,
`cake_batter`, `cake`, `carrot`, `cheese`, `cloth`, `corn_soup`, `corn`,
`egg_sandwich`, `egg`, `flour`, `meal`, `milk`, `pasta`, `sauce`, `soy_sauce`,
`soy_vegetables`, `soybean`, `tomato`, `wheat`, `wool`, `yarn`.

File inventory confirms 243 `item-single-ID-ANGLE.png` images and 243
`item-angles-ID-48-ANGLE.png` images (9 angles per ID in each set), plus 27
captures for each `item-v8-ID-1.png`, `-24.png`, and `-48.png` quantity. I opened
all generated contact sheets: `review/goods-single-01.jpg` through `-17.jpg`,
`review/goods-held-01.jpg` through `-17.jpg`, and `review/goods-counts-01.jpg`
through `-06.jpg`. The sheets were made from the originals with
`Tools/model_contact_sheet.py`; source captures were not changed. I also opened
native originals `item-v8-beef-1.png`, `item-v8-egg-1.png`, `item-v8-egg-24.png`,
`item-v8-egg-48.png`, `item-single-carrot-front.png`, and
`item-angles-carrot-48-front.png`.

Angles reviewed for each set: `front`, `right`, `rear`, `left`, `three-quarter`,
`top`, `rear-quarter`, `front-left`, and `rear-right`.

## Findings

- At qty 1, the gameplay captures show one selected product and the HUD reads
  `1/48`; the native egg example shows one egg in its carrier. At qty 24 and 48,
  the egg examples read `24/48` and `48/48` and add the corresponding units.
- Held 48-unit views keep the goods in upright, repeated stacks across the nine
  angles. I did not see obvious self-intersection, unsupported floating pieces,
  clipped goods, or a malformed orientation in the viewed angles. Materials and
  silhouettes remain distinguishable at the captured gameplay scale.
- The fragile IDs `egg`, `milk`, `bottled_milk`, `sauce`, and `soy_sauce` show
  product-specific carriers in the held views. The other goods are shown as
  direct stacks or as part of their own product model.
- No specific visual defect requiring correction was confirmed in this pass.

## Limits

This is a static-image review of the supplied captures and named angles. It does
not establish collision behavior, physics stability, or views outside the
captured set. HUD/world context remains in gameplay images but did not prevent
inspection of the goods in the opened captures.
