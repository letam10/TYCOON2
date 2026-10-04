"""Small rounded trees; CPU processing with no backup files."""
from pathlib import Path
import bpy,json,sys
sys.path.insert(0,str(Path(__file__).parent))
import process_assets as p
p.reset()
trunk=p.material('TreeTrunk',(.22,.09,.025));leaf=p.material('TreeLeaf',(.24,.64,.055));top=p.material('TreeLeafLight',(.42,.8,.075))
bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=.14,depth=1.1,location=(0,0,.55));p.finish(bpy.context.object,'Trunk',trunk,True)
for name,z,size,mat in [('Crown',1.65,(.78,.78,1.02),leaf),('CrownTop',2.1,(.55,.55,.7),top)]:
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=(0,0,z));obj=p.finish(bpy.context.object,name,mat,True);obj.scale=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
p.export('tree')
p.reset()
plate=p.material('Porcelain',(.94,.94,.85));meat=p.material('Steak',(.78,.1,.055));fat=p.material('Fat',(.92,.78,.65))
bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.32,depth=.055,location=(0,0,.028));p.finish(bpy.context.object,'Plate',plate,True)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=(0,0,.11));obj=p.finish(bpy.context.object,'Beef',meat,True);obj.scale=(.28,.22,.065);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
for y in (-.065,.055):p.box('Marbling',(0,y,.166),(.35,.025,.012),fat,.009)
p.export('beef')
path=p.ROOT/'QA/asset-audit.json';existing=json.loads(path.read_text(encoding='utf-8'));keys={x['key'] for x in p.REPORT};path.write_text(json.dumps([x for x in existing if x['key'] not in keys]+p.REPORT,indent=2),encoding='utf-8')
