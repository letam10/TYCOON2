"""Import selected user-authorized models, preserve sources, reduce and rig via Blender CLI."""
from pathlib import Path
import bpy, sys, math, json, shutil, hashlib, array
from mathutils import Vector, Quaternion
sys.path.insert(0, str(Path(__file__).parent))
import process_assets as pipeline

ROOT = Path(__file__).resolve().parents[1]
ORIGINAL = Path(r'D:\APP\TYCOON\ASSET')
SOURCE = ROOT / 'ASSET/UserProvided'
SOURCE.mkdir(parents=True, exist_ok=True)

def distance(point, a, b):
    delta=b-a
    return (point-(a+delta*max(0,min(1,(point-a).dot(delta)/delta.length_squared)))).length

def skin_parts(obj, hair=False):
    mesh=obj.data
    node=next((n for mat in mesh.materials if mat.use_nodes for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image),None)
    if not node or not mesh.uv_layers.active:return []
    image=node.image;pixels=array.array('f',[0])*len(image.pixels);image.pixels.foreach_get(pixels)
    selected=set();uv=mesh.uv_layers.active.data
    for loop in mesh.loops:
        u,v=uv[loop.index].uv;offset=(int(v*image.size[1])%image.size[1]*image.size[0]+int(u*image.size[0])%image.size[0])*4
        r,g,b=pixels[offset:offset+3]
        if hair:
            valid=r>.03 and .3<g/max(.001,r)<.82 and .2<b/max(.001,g)<.95 and mesh.vertices[loop.vertex_index].co.z>.8
        else:valid=r>.45 and g>.3 and .58<g/max(.001,r)<.95 and .38<b/max(.001,g)<.92
        if valid:selected.add(loop.vertex_index)
    parents=list(range(len(mesh.vertices)))
    def find(i):
        while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
        return i
    def join(a,b):
        a=find(a);b=find(b)
        if a!=b:parents[b]=a
    positions={}
    for i in selected:
        key=tuple(round(c,3) for c in mesh.vertices[i].co)
        if key in positions:join(i,positions[key])
        else:positions[key]=i
    for edge in mesh.edges:
        a,b=edge.vertices
        if a in selected and b in selected:join(a,b)
    grouped={}
    for i in selected:grouped.setdefault(find(i),[]).append(i)
    parts=[]
    for indices in grouped.values():
        if len(indices)<40:continue
        center=sum((obj.matrix_world@mesh.vertices[i].co for i in indices),Vector())/len(indices)
        if not hair or max(mesh.vertices[i].co.z for i in indices)>1.24:parts.append((indices,center))
    return sorted(parts,key=lambda part:len(part[0]),reverse=True)

def rig_character():
    # Rig nhỏ theo tỷ lệ đầu lớn của model; nhóm xương tránh kéo mặt vào tay.
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    parts_by_mesh={obj.name:skin_parts(obj) for obj in meshes}
    hair_by_mesh={obj.name:skin_parts(obj,True) for obj in meshes}
    hand_centers={}
    for parts in parts_by_mesh.values():
        for indices,center in parts:
            suffix='L' if center.x>=0 else 'R'
            if abs(center.x)>.2 and .4<center.z<1.12 and suffix not in hand_centers:hand_centers[suffix]=center
    bpy.ops.object.armature_add(location=(0,0,0))
    rig=bpy.context.object; rig.name='ToyRig'
    bpy.ops.object.mode_set(mode='EDIT'); rig.data.edit_bones.remove(rig.data.edit_bones[0])
    specifications=[('Hips',(0,0,.43),(0,0,.57),None),('Spine',(0,0,.57),(0,0,.74),'Hips'),('Chest',(0,0,.74),(0,0,.91),'Spine'),('Head',(0,0,.91),(0,0,1.68),'Chest')]
    for suffix,sign in [('L',1),('R',-1)]:
        specifications += [(f'Shoulder.{suffix}',(0,0,.84),(sign*.23,0,.84),'Chest'),(f'UpperArm.{suffix}',(sign*.23,0,.84),(sign*.35,0,.63),f'Shoulder.{suffix}'),(f'LowerArm.{suffix}',(sign*.35,0,.63),(sign*.43,-.13,.77),f'UpperArm.{suffix}'),(f'Hand.{suffix}',(sign*.43,-.13,.77),(sign*.46,-.18,.87),f'LowerArm.{suffix}'),(f'UpperLeg.{suffix}',(sign*.14,0,.46),(sign*.15,0,.25),'Hips'),(f'LowerLeg.{suffix}',(sign*.15,0,.25),(sign*.15,0,.08),f'UpperLeg.{suffix}'),(f'Foot.{suffix}',(sign*.15,0,.08),(sign*.15,-.18,.06),f'LowerLeg.{suffix}')]
    for name,head,tail,parent in specifications:
        b=rig.data.edit_bones.new(name); b.head=head; b.tail=tail
        if parent:b.parent=rig.data.edit_bones[parent]
    for suffix,center in hand_centers.items():
        sign=1 if suffix=='L' else -1
        shoulder=rig.data.edit_bones['Shoulder.'+suffix];shoulder.head=(0,0,.92);shoulder.tail=(sign*.23,0,.92)
        upper=rig.data.edit_bones['UpperArm.'+suffix];upper.head=shoulder.tail;upper.tail=(sign*.34,center.y*.2,.69 if center.z>.85 else .76)
        lower=rig.data.edit_bones['LowerArm.'+suffix];lower.head=upper.tail;lower.tail=(center.x,center.y,center.z+(.05 if center.z<.85 else -.065))
        hand=rig.data.edit_bones['Hand.'+suffix];hand.head=lower.tail;hand.tail=(center.x,center.y,center.z+(-.14 if center.z<.85 else .15))
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in meshes:
        world=obj.matrix_world.copy(); obj.parent=rig; obj.matrix_world=world
        bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        groups={b.name:obj.vertex_groups.new(name=b.name) for b in rig.data.bones}
        for vertex in obj.data.vertices:
            p=obj.matrix_world@vertex.co
            suffix='L' if p.x>=0 else 'R'
            def smooth(low,high,value):
                x=max(0,min(1,(value-low)/(high-low)));return x*x*(3-2*x)
            head=smooth(.81,.96,p.z)*(1-smooth(.32,.46,abs(p.x))*(1-smooth(.98,1.12,p.z)))
            if abs(p.x)>.28 and p.y<-.07 and p.z<1.18:head=0
            arm=(1-head)*smooth(.20,.34,abs(p.x))*smooth(.40,.58,p.z)
            leg=(1-head-arm)*(1-smooth(.42,.54,p.z))
            torso=max(0,1-head-arm-leg)
            regions=[(['Head'],head),([f'UpperArm.{suffix}',f'LowerArm.{suffix}',f'Hand.{suffix}'],arm),([f'UpperLeg.{suffix}',f'LowerLeg.{suffix}',f'Foot.{suffix}'],leg),(['Hips','Spine','Chest'],torso)]
            # Chuyển vùng mềm để mặt/tay không bị kéo giãn ở đường ranh nhóm xương.
            for names,influence in regions:
                if influence<.0001:continue
                weights=[1/max(.04,distance(p,rig.data.bones[n].head_local,rig.data.bones[n].tail_local))**2 for n in names];total=sum(weights)
                for n,w in zip(names,weights):groups[n].add([vertex.index],influence*w/total,'REPLACE')
        # Nhận dạng cụm da theo UV để bàn tay giơ cao không bị gán vào xương đầu.
        locked={}
        for indices,center in parts_by_mesh[obj.name]:
            target='Head' if abs(center.x)<.2 and center.z>1 else 'Hand.'+('L' if center.x>=0 else 'R') if abs(center.x)>.2 and .4<center.z<1.12 else None
            if target:
                for group in groups.values():group.remove(indices)
                groups[target].add(indices,1,'REPLACE')
                for index in indices:locked[index]=target
        for indices,center in hair_by_mesh[obj.name]:
            for group in groups.values():group.remove(indices)
            groups['Head'].add(indices,1,'REPLACE')
            for index in indices:locked[index]='Head'
        # Các đỉnh trùng tọa độ ở seam UV phải dùng cùng trọng số; FBX giữ tối đa 4 ảnh hưởng.
        seams={}
        for vertex in obj.data.vertices:seams.setdefault(tuple(round(c,5) for c in vertex.co),[]).append(vertex.index)
        for indices in seams.values():
            tags=[locked[i] for i in indices if i in locked]
            if tags:weights={max(set(tags),key=tags.count):1}
            else:
                weights={}
                for index in indices:
                    for member in obj.data.vertices[index].groups:
                        name=obj.vertex_groups[member.group].name;weights[name]=weights.get(name,0)+member.weight/len(indices)
                weights=dict(sorted(weights.items(),key=lambda pair:pair[1],reverse=True)[:4])
                total=sum(weights.values());weights={name:value/total for name,value in weights.items()}
            for group in groups.values():group.remove(indices)
            for name,value in weights.items():groups[name].add(indices,value,'REPLACE')
        modifier=obj.modifiers.new('Skin','ARMATURE');modifier.object=rig
    pipeline.humanoid_actions(rig)
    action=bpy.data.actions.new('Sit');rig.animation_data.action=action
    for frame in (1,24):
        for bone in rig.pose.bones:
            bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0)
            if bone.name.startswith('UpperLeg'):bone.rotation_quaternion=Quaternion((1,0,0),-math.pi/2)
            if bone.name.startswith('LowerLeg'):bone.rotation_quaternion=Quaternion((1,0,0),math.pi/2)
            bone.keyframe_insert(data_path='rotation_quaternion',frame=frame);bone.keyframe_insert(data_path='location',frame=frame)
    return rig

def texture_metadata(key):
    result={}
    for mat in bpy.data.materials:
        if not mat.use_nodes:continue
        bsdf=mat.node_tree.nodes.get('Principled BSDF')
        if not bsdf or not bsdf.inputs['Base Color'].is_linked:continue
        node=bsdf.inputs['Base Color'].links[0].from_node
        if node.type!='TEX_IMAGE' or not node.image:continue
        folder=ROOT/'Assets/_Game/Art/Imported/Textures';folder.mkdir(parents=True,exist_ok=True)
        target=folder/(key+'_basecolor.png')
        node.image.filepath_raw=str(target);node.image.file_format='PNG';node.image.save()
        result[mat.name]=str(target.relative_to(ROOT)).replace('\\','/')
        node.image.pack()
    return result

def rig_animal(height):
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    chicken=height<1
    bpy.ops.object.armature_add(location=(0,0,0));rig=bpy.context.object;rig.name='FarmRig'
    bpy.ops.object.mode_set(mode='EDIT');root=rig.data.edit_bones[0];root.name='Hips';root.head=(0,0,height*.3);root.tail=(0,0,height*.6)
    head=rig.data.edit_bones.new('Head');head.head=(0,-height*.22,height*.6);head.tail=(0,-height*.32,height*.9);head.parent=root
    legs=[]
    for sx in (-1,1):
        for sy in ((0,) if chicken else (-1,1)):
            name='Leg'+str(sx)+'_'+str(sy);legs.append((name,sx,sy))
            upper=rig.data.edit_bones.new(name);upper.head=(sx*height*(.12 if chicken else .27),sy*height*.3,height*.42);upper.tail=(upper.head.x,upper.head.y,height*.18);upper.parent=root
            lower=rig.data.edit_bones.new(name+'_Foot');lower.head=upper.tail;lower.tail=(upper.head.x,upper.head.y-.025,height*.045);lower.parent=upper
    bpy.ops.object.mode_set(mode='OBJECT')
    for obj in meshes:
        matrix=obj.matrix_world.copy();obj.parent=rig;obj.matrix_world=matrix
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        groups={bone.name:obj.vertex_groups.new(name=bone.name) for bone in rig.data.bones}
        for vertex in obj.data.vertices:
            point=vertex.co
            if point.z<height*.37 and (chicken or abs(point.x)>height*.12):
                sx=1 if point.x>=0 else -1;sy=0 if chicken else (1 if point.y>=0 else -1)
                name='Leg'+str(sx)+'_'+str(sy);foot=max(0,min(1,(height*.22-point.z)/(height*.15)))
                groups[name].add([vertex.index],1-foot,'REPLACE')
                if foot>0:groups[name+'_Foot'].add([vertex.index],foot,'REPLACE')
            else:
                weight=max(0,min(.8,(point.z-height*.58)/(height*.3))) if point.y<height*.12 else 0
                groups['Hips'].add([vertex.index],1-weight,'REPLACE')
                if weight>0:groups['Head'].add([vertex.index],weight,'REPLACE')
        mod=obj.modifiers.new('Skin','ARMATURE');mod.object=rig
    rig.animation_data_create()
    for action_name in ('Idle','Walk'):
        action=bpy.data.actions.new(action_name);rig.animation_data.action=action
        for frame in (1,4,7,10,13,16,19,22,25):
            phase=(frame-1)/24*math.tau
            for bone in rig.pose.bones:bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion();bone.location=(0,0,0)
            rig.pose.bones['Hips'].location=(0,0,math.sin(phase)*.006)
            rig.pose.bones['Head'].rotation_quaternion=Quaternion((0,1,0),math.sin(phase)*.012)
            if action_name=='Walk':
                for name,sx,sy in legs:
                    swing=phase+(math.pi if (sx>0) != (sy>0) else 0)
                    rig.pose.bones[name].rotation_quaternion=Quaternion((1,0,0),math.sin(swing)*.1)
                    rig.pose.bones[name+'_Foot'].rotation_quaternion=Quaternion((1,0,0),max(0,math.cos(swing))*.055)
            for bone in rig.pose.bones:bone.keyframe_insert(data_path='location',frame=frame);bone.keyframe_insert(data_path='rotation_quaternion',frame=frame)

sources=[]
for key,name,height,triangles,human in [('player','Character1',1.78,9500,True),('customer','Character2',1.78,7500,True),('customer_beach','Character3',1.78,7500,True),('user_cow','Cow',1.35,3500,False),('user_chicken','Chicken',.8,2500,False),('user_checkout','Machine2',1.15,5000,False)]:
    original=ORIGINAL/(name+'.glb'); target=SOURCE/original.name
    if original.read_bytes()[:4]!=b'glTF':raise ValueError('Not a GLB: '+str(original))
    shutil.copyfile(original,target)
    sha=hashlib.sha256(target.read_bytes()).hexdigest()
    sources.append({'file':str(target.relative_to(ROOT)),'original':str(original),'sha256':sha,'permission':'User explicitly authorized copying into TYCOON2; no separate publisher license supplied.'})
    pipeline.reset();bpy.ops.import_scene.gltf(filepath=str(target));pipeline.normalize(height)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    for obj in meshes:
        bpy.context.view_layer.objects.active=obj
        obj.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        obj.data.calc_loop_triangles(); count=len(obj.data.loop_triangles)
        if count>triangles:
            mod=obj.modifiers.new('GameReduction','DECIMATE');mod.ratio=triangles/count;mod.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    if human:rig_character()
    elif key in ('user_cow','user_chicken'):rig_animal(height)
    textures=texture_metadata(key)
    pipeline.export(key,target,human or key in ('user_cow','user_chicken'))
    for entry in pipeline.REPORT[-1]['materials']:
        if entry['name'] in textures:entry['texture']=textures[entry['name']]
reportPath=ROOT/'QA/asset-audit.json'
existing=json.loads(reportPath.read_text(encoding='utf-8'))
if isinstance(existing,dict):existing=existing['entries']
keys={x['key'] for x in pipeline.REPORT}
reportPath.write_text(json.dumps([x for x in existing if x['key'] not in keys]+pipeline.REPORT,indent=2),encoding='utf-8')
(SOURCE/'provenance.json').write_text(json.dumps(sources,indent=2),encoding='utf-8')
