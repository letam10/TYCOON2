using UnityEngine;

namespace Tycoon
{
    // Model mới dùng chung palette URP; không tạo inventory hoặc tác động state.
    public static class TownArt
    {
        public static bool Supports(string key)=>key is "corn_plant" or "soybean_plant" or "corn" or "soybean" or "animal_feed" or "soy_sauce" or "bottled_milk" or "wool" or "yarn" or "cloth" or "bread_dough" or "cake_batter" or "beef_soy" or "corn_soup" or "pasta" or "egg_sandwich" or "soy_vegetables" or "user_sheep" or "feedmill" or "soyextractor" or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer";
        static GameObject Ball(string name,Vector3 p,Vector3 size,string color,Transform parent)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=Art.Material(color);Object.Destroy(g.GetComponent<Collider>());return g;
        }
        public static GameObject Build(string key)
        {
            var root=new GameObject(key);var t=root.transform;
            if(key is "corn_plant" or "soybean_plant")
            {
                float height=key=="corn_plant"?1.6f:.65f;
                Art.Box("Stem",new(0,height*.5f,0),new(.05f,height,.05f),"#659349",t);
                for(int i=0;i<4;i++)
                {
                    var leaf=Art.Model("crop_leaves",new(0,.2f+i*height*.18f,0),t,key=="corn_plant"?.75f:.55f,i*90);
                    leaf.transform.localScale=new(.75f,.25f,.75f);
                }
                if(key=="corn_plant"){Art.Model("corn",new(.1f,.6f,0),t,.55f,45);Art.Model("wheat_crop",new(0,1.4f,0),t,.2f);}
                else for(int i=0;i<3;i++)Ball("Pod",new((i%2==0?-.16f:.16f),.2f+i*.12f,0),new(.11f,.2f,.09f),"#86A754",t);
            }
            else if(key=="user_sheep")
            {
                Ball("Body",new(0,.8f,0),new(.8f,.72f,1.15f),"#F0E8DA",t);
                for(int i=0;i<9;i++)Ball("Fleece",new((i%3-1)*.25f,.85f+(i%2)*.15f,(i/3-1)*.35f),Vector3.one*.4f,"#F4EDDE",t);
                Ball("Head",new(0,1,.7f),new(.38f,.5f,.4f),"#62564B",t);
                for(int i=0;i<4;i++)Art.Box("Leg",new(i%2==0?-.27f:.27f,.27f,i<2?-.35f:.35f),new(.13f,.55f,.14f),"#64594B",t);
                for(int i=0;i<2;i++){Ball("Ear",new(i==0?-.23f:.23f,1.07f,.65f),new(.26f,.12f,.13f),"#62564B",t);Ball("Eye",new(i==0?-.15f:.15f,1.12f,.86f),Vector3.one*.065f,"#151A19",t);}
            }
            else if(key is "feedmill" or "soyextractor" or "milkbottler" or "spinner" or "loom" or "breadmixer" or "cakemixer")
            {
                Art.Box("MachineBase",new(0,.22f,0),new(1.55f,.44f,1.2f),"#516D6B",t);
                string color=key is "spinner" or "loom"?"#967251":key=="milkbottler"?"#ECE8D9":"#ADC3BA";
                Art.Box("MachineBody",new(0,.8f,.18f),new(1.3f,.72f,.65f),color,t);
                if(key=="loom")
                {for(int i=0;i<8;i++)Art.Box("Warp",new(-.5f+i*.14f,.8f,-.3f),new(.04f,.5f,.45f),"#D9CDAA",t);Art.Cylinder("Rotor",new(0,1.12f,-.3f),new(.4f,.65f,.4f),"#719CAC",t).transform.rotation=Quaternion.Euler(0,0,90);}
                else
                {Art.Cylinder("Bowl",new(0,.9f,-.25f),new(.75f,.25f,.75f),"#CED4C9",t);Art.Cylinder("Rotor",new(0,1.1f,-.25f),new(.4f,.13f,.4f),"#7E9C92",t);Art.Box("Drive",new(.48f,1.25f,0),new(.24f,.4f,.24f),color,t);}
                Art.Box("ControlPanel",new(.55f,.75f,-.53f),new(.23f,.3f,.08f),"#233E3E",t);
                Ball("Light",new(.55f,.8f,-.58f),Vector3.one*.06f,"#92D973",t);
            }
            else if(key is "beef_soy" or "corn_soup" or "pasta" or "egg_sandwich" or "soy_vegetables")
            {
                Art.Cylinder("Plate",new(0,.04f,0),new(.53f,.04f,.53f),"#F1EBDD",t);
                if(key=="corn_soup")
                {Art.Cylinder("Bowl",new(0,.12f,0),new(.4f,.1f,.4f),"#D9CBB4",t);Art.Cylinder("Soup",new(0,.23f,0),new(.33f,.01f,.33f),"#EACB6A",t);for(int i=0;i<3;i++)Ball("Corn",new(-.1f+i*.1f,.25f,0),Vector3.one*.055f,"#E2B637",t);}
                else if(key=="egg_sandwich")
                {Art.Model("bread",new(0,.05f,0),t,.6f);Ball("EggWhite",new(0,.18f,0),new(.29f,.04f,.22f),"#F7F0D8",t);Ball("Yolk",new(0,.21f,0),new(.12f,.04f,.12f),"#EAC644",t);}
                else if(key=="pasta")
                {for(int i=0;i<7;i++)Art.Box("Noodle",new(-.1f+i*.033f,.1f,0),new(.02f,.045f,.27f),"#E2C787",t);Ball("Sauce",new(0,.15f,0),new(.23f,.045f,.19f),"#B65538",t);Ball("Cheese",new(0,.18f,0),new(.12f,.02f,.12f),"#EEE4B0",t);}
                else if(key=="beef_soy")
                {Art.Model("beef",new(0,.08f,0),t,.5f);for(int i=0;i<4;i++)Ball("Carrot",new((i%2-.5f)*.28f,.12f,(i/2-.5f)*.26f),new(.07f,.05f,.07f),"#CF803E",t);}
                else for(int i=0;i<6;i++)Ball("Vegetables",new((i%3-1)*.11f,.11f,(i/3-.5f)*.16f),new(.15f,.12f,.15f),i%2==0?"#80AB59":"#CEC286",t);
            }
            else if(key=="corn")
            {Ball("Cob",new(0,.3f,0),new(.23f,.6f,.23f),"#EBC44A",t);for(int i=0;i<5;i++)for(int j=0;j<5;j++)Ball("Kernel",new(Mathf.Cos(j*1.256f)*.095f,.12f+i*.08f,Mathf.Sin(j*1.256f)*.095f),Vector3.one*.075f,"#F5D65D",t);}
            else if(key=="soybean")
            {for(int i=0;i<5;i++)Ball("Bean",new((i%3-1)*.12f,.07f+(i/3)*.08f,(i/3-.5f)*.14f),new(.14f,.11f,.11f),"#C9C694",t);}
            else if(key is "soy_sauce" or "bottled_milk")
            {Art.Cylinder("Bottle",new(0,.23f,0),new(.24f,.23f,.24f),key=="soy_sauce"?"#653A25":"#EEE9D5",t);Art.Cylinder("Neck",new(0,.5f,0),new(.12f,.045f,.12f),"#558567",t);Art.Box("Label",new(0,.27f,-.122f),new(.16f,.15f,.015f),"#E8DFC8",t);}
            else if(key=="cloth")
            {for(int i=0;i<3;i++)Art.Box("Fold",new(0,.05f+i*.055f,0),new(.46f,.05f,.3f),i%2==0?"#83AFB9":"#6995A3",t);}
            else if(key=="yarn")
            {Art.Cylinder("Thread",new(0,.15f,0),new(.35f,.15f,.35f),"#D7BF8B",t);Art.Cylinder("Core",new(0,.15f,0),new(.09f,.2f,.09f),"#8C6240",t);}
            else
            {for(int i=0;i<3;i++)Ball("Bundle",new((i-1)*.12f,.18f,0),new(.29f,.32f,.3f),key=="wool"?"#EFE6D7":key=="animal_feed"?"#B5A16D":"#E8CF9E",t);}
            return root;
        }
    }
}
