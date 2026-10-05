using UnityEngine;

namespace Tycoon
{
    public static class FinalBusinesses
    {
        public static void Build(GameSession game, Transform world)
        {
            var market = WorldFactory.Floor(world, "04  SIÊU THỊ", new Vector3(28,0,33), new Vector2(22,22));
            market.gameObject.AddComponent<UnlockVisual>().Requirement = "supermarket";
            WorldFactory.Shelf(game,world,"shelf_market_1","RAU CỦ",new Vector3(36.8f,0,38),new[]{"carrot","tomato","wheat"},"market","supermarket");
            WorldFactory.Shelf(game,world,"shelf_market_2","THỊT & SỮA",new Vector3(36.8f,0,33),new[]{"milk","egg","cheese","beef"},"market","supermarket");
            WorldFactory.Shelf(game,world,"shelf_market_3","CHẾ BIẾN",new Vector3(36.8f,0,28),new[]{"flour","sauce","bread","cake"},"market","supermarket");
            WorldFactory.Checkout(game,world,"checkout_market",new Vector3(29,0,23),"market","supermarket");
            WorldFactory.Checkout(game,world,"checkout_market_2",new Vector3(20,0,23),"market","supermarket");
            var bakery = WorldFactory.Floor(world,"05  TIỆM BÁNH",new Vector3(4,0,38),new Vector2(20,20));
            bakery.gameObject.AddComponent<UnlockVisual>().Requirement="bakery";
            WorldFactory.Machine(game,world,"oven",new Vector3(0,0,38),"bakery");
            WorldFactory.Machine(game,world,"cakeoven",new Vector3(6,0,38),"bakery");
            WorldFactory.Shelf(game,world,"shelf_bakery","BÁNH MÌ & BÁNH NGỌT",new Vector3(11.8f,0,37),new[]{"bread","cake"},"bakery","bakery");
            WorldFactory.Checkout(game,world,"checkout_bakery",new Vector3(7,0,27),"bakery","bakery");
            var restaurant = WorldFactory.Floor(world,"05  NHÀ HÀNG",new Vector3(-20,0,39),new Vector2(22,20));
            restaurant.gameObject.AddComponent<UnlockVisual>().Requirement="restaurant";
            WorldFactory.Machine(game,world,"kitchen",new Vector3(-18,0,45),"restaurant");
            WorldFactory.Checkout(game,world,"checkout_restaurant",new Vector3(-12,0,29),"restaurant","restaurant");
            for(int i=0;i<6;i++)
            {
                var root=new GameObject("table_"+i);root.transform.SetParent(world);root.transform.position=new Vector3(-27+i%3*5,0,34+i/3*6);
                var table=root.AddComponent<TableStation>();table.Id=root.name;table.Label="Bàn "+(i+1);table.Requirement="restaurant";table.AreaId="restaurant";table.Inventory=new Inventory(1);
                table.InteractionPoint=root.transform.position+Vector3.back*1.6f;table.Seat=root.transform.position+Vector3.right*1.4f;
                WorldDressing.Table(table);
                Art.Box("TableCollision",new Vector3(0,.5f,0),new Vector3(1.55f,1.25f,1.55f),"#FFFFFF",root.transform,true).GetComponent<Renderer>().enabled=false;
                table.StatusLabel=Art.Label(table.Label,new Vector3(0,2,0),root.transform,.2f,"#FFFFFF");
                WorldFactory.Zone(game,world,table,"operate",table.InteractionPoint+Vector3.right*1.5f);
                WorldFactory.Zone(game,world,table,"serve",table.InteractionPoint+Vector3.left*1.5f);
                root.AddComponent<UnlockVisual>().Requirement="restaurant";game.Tables.Add(table);game.Stations.Add(table);
            }
            foreach(string id in new[]{"supermarket","restocker_market","cashier_market","bakery","cook_bakery","restaurant","cook","waiter"})
            {
                Vector3 point=id switch {
                    "supermarket"=>new Vector3(21,0,19),"restocker_market"=>new Vector3(21,0,27),"cashier_market"=>new Vector3(25,0,27),
                    "bakery"=>new Vector3(2,0,24),"cook_bakery"=>new Vector3(2,0,32),"restaurant"=>new Vector3(-32,0,30),
                    "cook"=>new Vector3(-20,0,31),_=>new Vector3(-25,0,31)};
                WorldFactory.Pad(game,world,Definitions.Upgrade(id),point);
            }
        }
    }
}
