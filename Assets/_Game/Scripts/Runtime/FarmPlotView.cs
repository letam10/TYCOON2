using UnityEngine;

namespace Tycoon
{
    public sealed class FarmPlotView : MonoBehaviour
    {
        MeshRenderer soil;
        Mesh soilMesh;
        MaterialPropertyBlock soilProperties;
        readonly MeshRenderer[] phaseLights = new MeshRenderer[4];
        Transform readyMarker;
        ProductionStation plot;
        int phase=-1;
        float wetness=-1;
        public void Build()
        {
            if (soil) return;
            plot=GetComponent<ProductionStation>();
            if (!plot || plot.Animal) return;
            var root=new GameObject("FurrowSoil",typeof(MeshFilter),typeof(MeshRenderer));root.transform.SetParent(transform,false);
            var vertices=new Vector3[33*33];var triangles=new int[32*32*6];var uv=new Vector2[vertices.Length];
            for(int z=0;z<=32;z++)for(int x=0;x<=32;x++)
            {
                float px=x/8f-2,pz=z/8f-2;
                float edge=Mathf.Clamp01((2-Mathf.Abs(px))*4)*Mathf.Clamp01((2-Mathf.Abs(pz))*4);
                vertices[z*33+x]=new Vector3(px,.08f+edge*(.05f+.045f*Mathf.Cos(px*Mathf.PI*4)),pz);
                uv[z*33+x]=new Vector2(x/32f,z/32f);
            }
            int t=0;for(int z=0;z<32;z++)for(int x=0;x<32;x++)
            {int a=z*33+x;triangles[t++]=a;triangles[t++]=a+33;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=a+33;triangles[t++]=a+34;}
            soilMesh=new Mesh{name="SquareFurrows"};soilMesh.vertices=vertices;soilMesh.triangles=triangles;soilMesh.uv=uv;soilMesh.RecalculateNormals();soilMesh.RecalculateBounds();
            root.GetComponent<MeshFilter>().sharedMesh=soilMesh;soil=root.GetComponent<MeshRenderer>();soil.sharedMaterial=Art.Material("#8B603E");
            soilProperties=new MaterialPropertyBlock();
            Art.Box("PlotBankNorth",new(0,.09f,2),new(4.2f,.18f,.18f),"#B79A68",transform);
            Art.Box("PlotBankSouth",new(0,.09f,-2),new(4.2f,.18f,.18f),"#B79A68",transform);
            Art.Box("PlotBankWest",new(-2,.09f,0),new(.18f,.18f,4),"#B79A68",transform);
            Art.Box("PlotBankEast",new(2,.09f,0),new(.18f,.18f,4),"#B79A68",transform);
            for(int i=0;i<4;i++)
            {
                var pin=Art.Cylinder("PlotPhase"+i,new(-.42f+i*.28f,.21f,-2),new(.09f,.035f,.09f),"#D3C5A2",transform);
                phaseLights[i]=pin.GetComponent<MeshRenderer>();
            }
            readyMarker=Art.Box("HarvestReady",new(1.75f,.5f,-1.75f),Vector3.one*.16f,"#FFD875",transform).transform;
            readyMarker.localRotation=Quaternion.Euler(0,45,45);readyMarker.gameObject.SetActive(false);
            Refresh();
        }
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(!plot||!soil)return;
            int current=plot.Phase;
            float moisture=current is 2 or 3?1:current==1?Mathf.Clamp01(plot.Action):0;
            if(!Mathf.Approximately(wetness,moisture))
            {
                wetness=moisture;
                // Đất ẩm chỉ phản ánh tiến độ tưới; view không viết vào transaction.
                soilProperties.SetColor("_BaseColor",Color.Lerp(Art.Hex("#98734E"),Art.Hex("#624936"),moisture).linear);
                soilProperties.SetFloat("_Smoothness",Mathf.Lerp(.08f,.3f,moisture));soil.SetPropertyBlock(soilProperties);
            }
            if(phase!=current)
            {
                phase=current;
                for(int i=0;i<phaseLights.Length;i++)phaseLights[i].sharedMaterial=Art.Material(i==phase?i switch{0=>"#FFD875",1=>"#8DCFDA",2=>"#9AC88C",_=>"#EEC96B"}:"#D3C5A2");
                readyMarker.gameObject.SetActive(phase==3);
            }
            if(phase==3)
            {
                readyMarker.localPosition=new(1.75f,.48f+Mathf.Sin(Time.time*3.5f)*.07f,-1.75f);
                readyMarker.localRotation=Quaternion.Euler(0,Time.time*70,45);
            }
        }
        void OnDestroy(){if(soilMesh){if(Application.isPlaying)Destroy(soilMesh);else DestroyImmediate(soilMesh);}}
    }
}
