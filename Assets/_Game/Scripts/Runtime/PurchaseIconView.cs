using UnityEngine;
using UnityEngine.Rendering;

namespace Tycoon
{
    // Icon tự vẽ bằng nét và mảng màu; không dùng model hay sửa trạng thái mua.
    public sealed class PurchaseIconView : MonoBehaviour
    {
        SpriteRenderer display;
        Texture2D texture;
        Sprite sprite;
        string shown;
        public void SetUpgrade(UpgradeDefinition upgrade)
        {
            if(shown==upgrade.id)return;
            shown=upgrade.id;
            if(!display)
            {
                display=gameObject.AddComponent<SpriteRenderer>();
                display.shadowCastingMode=ShadowCastingMode.Off;display.receiveShadows=false;
            }
            ReleaseImage();
            texture=new Texture2D(DrawnPurchaseIcons.Size,DrawnPurchaseIcons.Size,TextureFormat.RGBA32,false)
            {name="Purchase_"+shown,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(DrawnPurchaseIcons.Draw(upgrade));
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),DrawnPurchaseIcons.Size,0,SpriteMeshType.FullRect);
            texture.Apply(false,true);
            sprite.name="Purchase_"+shown;display.sprite=sprite;
        }
        void LateUpdate(){if(Camera.main)transform.rotation=Camera.main.transform.rotation;}
        void ReleaseImage()
        {
            if(display)display.sprite=null;
            if(sprite){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}
            if(texture){if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
        }
        void OnDestroy()=>ReleaseImage();
    }

    public static class DrawnPurchaseIcons
    {
        public const int Size=128;
        static readonly Color32 Ink=new(38,64,56,255),Cream=new(255,244,219,255),Green=new(109,167,112,255),Blue=new(104,166,194,255),Gold=new(237,184,80,255),Orange=new(232,135,71,255),White=new(255,254,245,255),Brown=new(159,111,70,255),Skin=new(239,191,148,255);
        public static string Subject(UpgradeDefinition u)
        {
            if(u.kind=="worker")return u.role switch
            {
                "Farmer"=>"farmer","Restocker" or "RestockerMarket"=>"restocker","Cashier" or "CashierMarket"=>"cashier",
                "Transporter"=>"transporter","Animal" or "AnimalWorker"=>"animal_worker","Processor"=>"processor",
                "Baker"=>"baker","Cook"=>"cook","Waiter"=>"waiter","Repairer"=>"repairer","Loader"=>"loader","Driver"=>"driver",_=>"upgrade"
            };
            if(u.family.StartsWith("storage_"))return "crate";
            if(u.family.Length>0)return u.family switch
            {"farm"=>"carrot","animal"=>"cow","counter"=>"counter","mill"=>"mill","market"=>"market","oven"=>"oven","kitchen"=>"kitchen","player"=>"basket",_=>"upgrade"};
            return u.id switch
            {
                "barn"=>"cow","milk_line"=>"milk","egg_line"=>"eggs","sheep_line"=>"sheep","farm_shop"=>"shop",
                "mill"=>"mill","dairy"=>"cheese","supermarket"=>"market","bakery"=>"bread","restaurant"=>"meal",
                "conveyor_processing" or "conveyor_bakery"=>"conveyor",_=>"upgrade"
            };
        }
        public static Color32[] Draw(UpgradeDefinition upgrade)
        {
            var pixels=new Color32[Size*Size];var canvas=new Pen(pixels,0,0,1);
            canvas.Circle(64,64,58,Ink);canvas.Circle(64,65,54,Cream);
            var pen=canvas.At(62,68,1);string subject=Subject(upgrade);
            if(upgrade.kind=="worker")Worker(pen,subject);else ObjectIcon(pen,subject);
            if(upgrade.axisLevel>0)
                for(int i=0;i<Mathf.Min(4,upgrade.axisLevel);i++)canvas.Circle(44+i*10,17,3.2f,Green);
            Badge(canvas.At(102,25,1),upgrade.axis);
            return pixels;
        }
        static void ObjectIcon(Pen p,string subject)
        {
            switch(subject)
            {
                case "carrot":Carrot(p);break;case "cow":Cow(p);break;case "milk":Milk(p);break;
                case "eggs":Eggs(p);break;case "sheep":Sheep(p);break;case "shop":Shop(p);break;
                case "mill":Mill(p);break;case "cheese":Cheese(p);break;case "market":Cart(p);break;
                case "oven":Oven(p);break;case "kitchen":Pot(p);break;case "basket":Basket(p);break;
                case "crate":Crate(p);break;case "counter":Counter(p);break;case "conveyor":Conveyor(p);break;
                case "bread":Bread(p);break;case "meal":Meal(p);break;default:Arrow(p,Green);break;
            }
        }
        static void Carrot(Pen p)
        {
            p.Poly(Ink,new(-19,13),new(20,9),new(-8,-31));p.Poly(Orange,new(-15,10),new(16,7),new(-7,-26));
            p.Line(-1,13,-13,32,7,Green);p.Line(2,14,5,36,7,Green);p.Line(5,14,19,29,7,Green);
            p.Line(-12,1,-1,-1,3,Brown);p.Line(-9,-9,0,-10,3,Brown);
        }
        static void Cow(Pen p)
        {
            p.Line(-19,24,-29,31,7,Gold);p.Line(19,24,29,31,7,Gold);
            p.Ellipse(-23,15,14,8,Ink);p.Ellipse(23,15,14,8,Ink);
            p.Ellipse(-23,16,11,5,White);p.Ellipse(23,16,11,5,White);
            p.Ellipse(0,3,25,29,Ink);p.Ellipse(0,5,21,25,White);p.Ellipse(-9,18,9,10,Brown);
            p.Ellipse(0,-14,20,12,Ink);p.Ellipse(0,-13,17,9,Skin);
            p.Circle(-10,6,2.7f,Ink);p.Circle(10,6,2.7f,Ink);p.Circle(-7,-13,2,Ink);p.Circle(7,-13,2,Ink);
        }
        static void Milk(Pen p)
        {
            p.Poly(Ink,new(-20,-29),new(20,-29),new(20,11),new(10,23),new(10,31),new(-10,31),new(-10,23),new(-20,11));
            p.Poly(White,new(-16,-25),new(16,-25),new(16,10),new(7,21),new(-7,21),new(-16,10));
            p.Rect(-11,27,22,7,Blue);p.Rect(-16,-9,32,19,Blue);p.Ellipse(0,1,6,8,White);
        }
        static void Eggs(Pen p)
        {
            p.Rect(-34,-25,68,12,Brown);
            foreach(float x in new[]{-20f,0,20}){p.Ellipse(x,0,14,24,Ink);p.Ellipse(x,1,11,21,White);p.Ellipse(x-3,6,3,9,Cream);}
            p.Line(-30,-22,30,-22,3,Gold);
        }
        static void Sheep(Pen p)
        {
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;p.Circle(Mathf.Cos(a)*19,Mathf.Sin(a)*15+4,15,Ink);}
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;p.Circle(Mathf.Cos(a)*19,Mathf.Sin(a)*15+5,12,White);}
            p.Ellipse(0,0,18,25,Ink);p.Ellipse(0,1,14,22,Brown);p.Circle(-7,8,2.4f,White);p.Circle(7,8,2.4f,White);
            p.Circle(-8,27,9,White);p.Circle(7,27,10,White);
        }
        static void Shop(Pen p)
        {
            p.Rect(-31,-28,62,47,Ink);p.Rect(-27,-24,54,39,Gold);p.Rect(-21,-22,19,25,Blue);p.Rect(8,-24,14,32,White);
            p.Poly(Ink,new(-37,14),new(37,14),new(28,33),new(-28,33));
            p.Poly(Green,new(-32,17),new(32,17),new(25,29),new(-25,29));
            for(int i=0;i<5;i++)p.Rect(-30+i*13,10,9,8,i%2==0?White:Green);
        }
        static void Gear(Pen p)
        {
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;p.Line(Mathf.Cos(a)*12,Mathf.Sin(a)*12,Mathf.Cos(a)*25,Mathf.Sin(a)*25,10,Ink);}
            p.Circle(0,0,22,Ink);p.Circle(0,0,17,Blue);p.Circle(0,0,8,Ink);p.Circle(0,0,4,Cream);
        }
        static void Mill(Pen p)
        {
            p.Rect(-29,-28,58,49,Ink);p.Rect(-25,-24,50,41,Blue);p.Poly(Gold,new(-22,23),new(22,23),new(9,11),new(-9,11));
            Gear(p.At(0,-1,.65f));p.Rect(-25,-32,9,5,Ink);p.Rect(16,-32,9,5,Ink);
        }
        static void Cheese(Pen p)
        {
            p.Poly(Ink,new(-31,-22),new(31,-22),new(31,19),new(-15,31),new(-31,12));
            p.Poly(Gold,new(-27,-18),new(27,-18),new(27,16),new(-14,27),new(-27,10));
            p.Line(-27,10,27,16,3,Orange);p.Circle(-13,-5,5,Orange);p.Circle(10,2,6,Orange);p.Circle(18,-12,3,Orange);
        }
        static void Cart(Pen p)
        {
            p.Line(-34,30,-24,30,5,Ink);p.Line(-24,30,-12,-23,5,Ink);p.Line(-12,-23,28,-23,5,Ink);
            p.Poly(Ink,new(-21,20),new(35,20),new(25,-10),new(-14,-10));p.Poly(Green,new(-16,16),new(29,16),new(22,-6),new(-10,-6));
            p.Line(0,15,3,-5,3,Cream);p.Line(16,15,15,-5,3,Cream);p.Line(-14,5,27,5,3,Cream);
            p.Circle(-8,-29,7,Ink);p.Circle(24,-29,7,Ink);p.Circle(-8,-29,3,Gold);p.Circle(24,-29,3,Gold);
        }
        static void Oven(Pen p)
        {
            p.Rect(-31,-29,62,59,Ink);p.Rect(-27,-25,54,51,Blue);p.Rect(-22,-18,44,29,Ink);p.Rect(-18,-14,36,21,Brown);
            Bread(p.At(0,-4,.43f));p.Circle(-17,21,3,Gold);p.Circle(-5,21,3,Gold);p.Line(8,20,21,20,3,White);
        }
        static void Pot(Pen p)
        {
            p.Line(-32,8,32,8,7,Ink);p.Ellipse(0,-7,26,21,Ink);p.Ellipse(0,-5,22,17,Blue);p.Rect(-26,3,52,7,Ink);
            p.Line(-17,17,-12,23,3,Green);p.Line(-12,23,-15,30,3,Green);p.Line(3,18,8,26,3,Green);p.Line(8,26,5,33,3,Green);
        }
        static void Basket(Pen p)
        {
            p.Ellipse(0,15,25,25,Ink);p.Ellipse(0,15,20,20,Cream);
            p.Poly(Ink,new(-34,8),new(34,8),new(24,-28),new(-24,-28));p.Poly(Gold,new(-28,4),new(28,4),new(20,-23),new(-20,-23));
            p.Line(-25,-5,25,-5,3,Brown);p.Line(-22,-15,22,-15,3,Brown);p.Line(-11,2,-8,-23,3,Brown);p.Line(11,2,8,-23,3,Brown);
        }
        static void Crate(Pen p)
        {
            p.Rect(-30,-27,60,54,Ink);p.Rect(-26,-23,52,46,Brown);p.Rect(-26,13,52,7,Gold);p.Rect(-26,-20,52,7,Gold);
            p.Line(-23,-16,23,16,6,Gold);p.Line(-23,16,23,-16,6,Gold);
        }
        static void Counter(Pen p)
        {
            p.Rect(-32,-28,64,25,Ink);p.Rect(-28,-24,56,17,Green);p.Rect(-19,-2,38,23,Ink);p.Rect(-15,2,30,15,Blue);
            p.Rect(-14,20,28,11,Ink);p.Rect(-10,23,20,5,Gold);p.Circle(22,10,10,Gold);p.Line(22,4,22,16,3,Ink);
        }
        static void Conveyor(Pen p)
        {
            p.Line(-29,-17,29,-17,20,Ink);p.Line(-29,-17,29,-17,12,Blue);
            for(int i=0;i<5;i++)p.Line(-25+i*12,-22,-25+i*12,-12,3,Cream);
            Crate(p.At(-16,9,.38f));Crate(p.At(18,9,.38f));p.Line(-20,-26,-20,-32,5,Ink);p.Line(20,-26,20,-32,5,Ink);
        }
        static void Bread(Pen p)
        {
            p.Ellipse(0,0,34,23,Ink);p.Ellipse(0,2,30,20,Brown);p.Ellipse(0,7,28,13,Gold);
            for(int i=0;i<3;i++)p.Line(-19+i*16,10,-12+i*16,-3,4,Cream);
        }
        static void Meal(Pen p)
        {
            p.Ellipse(0,-3,34,25,Ink);p.Ellipse(0,-1,30,21,White);p.Ellipse(0,-1,22,15,Cream);
            p.Ellipse(-6,1,14,9,Orange);p.Circle(15,3,6,Green);p.Circle(12,-7,6,Green);p.Line(-43,-27,-43,24,4,Ink);p.Line(43,-27,43,24,4,Ink);
            for(int i=0;i<3;i++)p.Line(-49+i*6,16,-49+i*6,29,3,Ink);
        }
        static void Truck(Pen p)
        {
            p.Rect(-36,-18,42,38,Ink);p.Rect(-32,-14,34,30,Gold);
            p.Poly(Ink,new(4,-18),new(38,-18),new(38,2),new(25,18),new(4,18));p.Poly(Blue,new(9,-13),new(33,-13),new(33,1),new(23,13),new(9,13));
            p.Rect(12,2,12,9,White);p.Circle(-22,-21,10,Ink);p.Circle(25,-21,10,Ink);p.Circle(-22,-21,5,Cream);p.Circle(25,-21,5,Cream);
        }
        static void Worker(Pen p,string role)
        {
            if(role=="driver"){Truck(p.At(0,-7,.9f));p.Circle(-19,30,10,Skin);p.Rect(-30,36,23,5,Blue);return;}
            bool chef=role is "cook" or "baker";
            p.Rect(-28,-29,37,32,Ink);p.Rect(-24,-25,29,25,chef?White:Blue);
            p.Line(-20,-18,-20,-2,5,Green);p.Line(1,-18,1,-2,5,Green);
            p.Ellipse(-10,19,16,19,Ink);p.Ellipse(-10,20,13,16,Skin);p.Circle(-15,21,2,Ink);p.Circle(-4,21,2,Ink);p.Line(-13,11,-6,11,2,Brown);
            if(chef){p.Rect(-25,33,30,9,White);p.Circle(-23,44,9,White);p.Circle(-10,48,12,White);p.Circle(3,44,9,White);}
            else if(role=="farmer"){p.Rect(-34,32,48,6,Brown);p.Rect(-24,36,29,11,Gold);}
            else{p.Rect(-27,34,34,6,role is "repairer" or "loader"?Gold:Green);p.Rect(-23,38,26,8,role is "repairer" or "loader"?Gold:Green);}
            var job=p.At(26,-3,.48f);
            switch(role)
            {
                case "farmer":Carrot(job);break;case "animal_worker":Cow(job);break;case "cashier":Counter(job);break;
                case "processor":Gear(job);break;case "baker":Bread(job);break;case "cook":Pot(job);break;
                case "waiter":Meal(job);break;case "transporter":Cart(job);break;case "loader":Crate(job);break;
                case "restocker":job.Rect(-31,-24,62,7,Ink);job.Rect(-31,0,62,5,Ink);Crate(job.At(-16,16,.43f));Crate(job.At(16,16,.43f));break;
                case "repairer":job.Line(-23,-24,18,17,12,Ink);job.Line(-23,-24,18,17,7,Blue);job.Line(15,16,11,30,7,Ink);job.Line(15,16,30,18,7,Ink);break;
            }
        }
        static void Arrow(Pen p,Color32 color)=>p.Poly(color,new(-20,-2),new(-7,-2),new(-7,-23),new(7,-23),new(7,-2),new(20,-2),new(0,23));
        static void Badge(Pen p,UpgradeAxis axis)
        {
            p.Circle(0,0,22,Ink);p.Circle(0,1,18,axis==UpgradeAxis.QualityValue?Gold:axis==UpgradeAxis.Speed?Blue:Green);
            switch(axis)
            {
                case UpgradeAxis.Speed:p.Circle(0,0,12,White);p.Circle(0,0,9,Blue);p.Rect(-4,12,8,4,White);p.Line(0,0,0,7,3,White);p.Line(0,0,6,-3,3,White);break;
                case UpgradeAxis.QualityValue:
                    var star=new Vector2[10];for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;float r=i%2==0?14:6;star[i]=new(Mathf.Cos(a)*r,Mathf.Sin(a)*r);}p.Poly(White,star);break;
                case UpgradeAxis.Capacity:p.Rect(-12,-8,24,16,White);p.Rect(-9,-5,18,10,Green);p.Line(0,-5,0,7,3,White);p.Line(-9,0,9,0,3,White);break;
                case UpgradeAxis.StationLevel:Arrow(p.At(0,0,.55f),White);break;
                default:p.Line(-10,0,10,0,4,White);p.Line(0,-10,0,10,4,White);break;
            }
        }
        // Trình vẽ dùng tọa độ icon; chỉ chạy khi tạo icon hoặc đổi định nghĩa nâng cấp.
        sealed class Pen
        {
            readonly Color32[] pixels;readonly float x,y,scale;
            public Pen(Color32[] pixels,float x,float y,float scale){this.pixels=pixels;this.x=x;this.y=y;this.scale=scale;}
            public Pen At(float px,float py,float size)=>new(pixels,x+px*scale,y+py*scale,scale*size);
            void Put(int px,int py,Color32 color){if(px>=0&&px<Size&&py>=0&&py<Size)pixels[py*Size+px]=color;}
            public void Rect(float px,float py,float width,float height,Color32 color)
            {int left=Mathf.FloorToInt(x+px*scale),bottom=Mathf.FloorToInt(y+py*scale),right=Mathf.CeilToInt(x+(px+width)*scale),top=Mathf.CeilToInt(y+(py+height)*scale);for(int iy=bottom;iy<top;iy++)for(int ix=left;ix<right;ix++)Put(ix,iy,color);}
            public void Circle(float px,float py,float radius,Color32 color)=>Ellipse(px,py,radius,radius,color);
            public void Ellipse(float px,float py,float rx,float ry,Color32 color)
            {
                float cx=x+px*scale,cy=y+py*scale;rx*=scale;ry*=scale;
                for(int iy=Mathf.FloorToInt(cy-ry);iy<=Mathf.CeilToInt(cy+ry);iy++)for(int ix=Mathf.FloorToInt(cx-rx);ix<=Mathf.CeilToInt(cx+rx);ix++)
                    if(Mathf.Pow((ix+.5f-cx)/rx,2)+Mathf.Pow((iy+.5f-cy)/ry,2)<=1)Put(ix,iy,color);
            }
            public void Line(float ax,float ay,float bx,float by,float width,Color32 color)
            {
                Vector2 a=new(x+ax*scale,y+ay*scale),b=new(x+bx*scale,y+by*scale),delta=b-a;float radius=width*scale*.5f;
                for(int iy=Mathf.FloorToInt(Mathf.Min(a.y,b.y)-radius);iy<=Mathf.CeilToInt(Mathf.Max(a.y,b.y)+radius);iy++)
                    for(int ix=Mathf.FloorToInt(Mathf.Min(a.x,b.x)-radius);ix<=Mathf.CeilToInt(Mathf.Max(a.x,b.x)+radius);ix++)
                    {Vector2 point=new(ix+.5f,iy+.5f);float t=delta.sqrMagnitude==0?0:Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude);if((point-a-delta*t).sqrMagnitude<=radius*radius)Put(ix,iy,color);}
            }
            public void Poly(Color32 color,params Vector2[] points)
            {
                float left=Size,right=0,bottom=Size,top=0;
                for(int i=0;i<points.Length;i++){points[i]=new(x+points[i].x*scale,y+points[i].y*scale);left=Mathf.Min(left,points[i].x);right=Mathf.Max(right,points[i].x);bottom=Mathf.Min(bottom,points[i].y);top=Mathf.Max(top,points[i].y);}
                for(int iy=Mathf.FloorToInt(bottom);iy<=Mathf.CeilToInt(top);iy++)for(int ix=Mathf.FloorToInt(left);ix<=Mathf.CeilToInt(right);ix++)
                {
                    bool inside=false;float px=ix+.5f,py=iy+.5f;
                    for(int i=0,j=points.Length-1;i<points.Length;j=i++)
                        if((points[i].y>py)!=(points[j].y>py)&&px<(points[j].x-points[i].x)*(py-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
                    if(inside)Put(ix,iy,color);
                }
            }
        }
    }
}
