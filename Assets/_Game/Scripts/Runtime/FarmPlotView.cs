using UnityEngine;

namespace Tycoon
{
    public sealed class FarmPlotView : MonoBehaviour
    {
        MeshRenderer soil;
        ProductionStation plot;
        int phase=-1;
        public void Build()
        {
            plot=GetComponent<ProductionStation>();
            var root=new GameObject("FurrowSoil",typeof(MeshFilter),typeof(MeshRenderer));root.transform.SetParent(transform,false);
            var vertices=new Vector3[33*33];var triangles=new int[32*32*6];
            for(int z=0;z<=32;z++)for(int x=0;x<=32;x++)
            {
                float px=x/8f-2,pz=z/8f-2;
                float edge=Mathf.Clamp01((2-Mathf.Abs(px))*4)*Mathf.Clamp01((2-Mathf.Abs(pz))*4);
                vertices[z*33+x]=new Vector3(px,.08f+edge*(.05f+.045f*Mathf.Cos(px*Mathf.PI*4)),pz);
            }
            int t=0;for(int z=0;z<32;z++)for(int x=0;x<32;x++)
            {int a=z*33+x;triangles[t++]=a;triangles[t++]=a+33;triangles[t++]=a+1;triangles[t++]=a+1;triangles[t++]=a+33;triangles[t++]=a+34;}
            var mesh=new Mesh{name="SquareFurrows"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
            root.GetComponent<MeshFilter>().sharedMesh=mesh;soil=root.GetComponent<MeshRenderer>();soil.sharedMaterial=Art.Material("#8B603E");
            Art.Box("PlotBankNorth",new(0,.09f,2),new(4.2f,.18f,.18f),"#957249",transform);
            Art.Box("PlotBankSouth",new(0,.09f,-2),new(4.2f,.18f,.18f),"#957249",transform);
            Art.Box("PlotBankWest",new(-2,.09f,0),new(.18f,.18f,4),"#957249",transform);
            Art.Box("PlotBankEast",new(2,.09f,0),new(.18f,.18f,4),"#957249",transform);
        }
        void LateUpdate(){if(plot&&soil&&phase!=plot.Phase){phase=plot.Phase;soil.sharedMaterial=Art.Material(phase==2?"#67452D":"#8B603E");}}
        void OnDestroy(){if(soil)Destroy(soil.GetComponent<MeshFilter>().sharedMesh);}
    }
}
