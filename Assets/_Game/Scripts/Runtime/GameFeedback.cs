using UnityEngine;

namespace Tycoon
{
    public sealed class GameFeedback : MonoBehaviour
    {
        AudioSource source;
        AudioClip pickup, sale;
        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .18f;
            pickup = Tone("Pickup", 580, .07f); sale = Tone("Sale", 880, .15f);
        }
        static AudioClip Tone(string name, float frequency, float seconds)
        {
            int count = (int)(44100 * seconds); var samples = new float[count];
            for (int i = 0; i < count; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100) * (1 - (float)i / count) * .5f;
            var clip = AudioClip.Create(name, count, 1, 44100, false); clip.SetData(samples, 0); return clip;
        }
        public void PlayPickup() => source.PlayOneShot(pickup);
        public void PlaySale() => source.PlayOneShot(sale);
        public void Burst(Vector3 point)
        {
            var root=new GameObject("PurchaseBurst");root.transform.position=point;
            for(int i=0;i<8;i++){float angle=i*Mathf.PI/4;Art.Box("Spark",new Vector3(Mathf.Cos(angle),.3f,Mathf.Sin(angle)),Vector3.one*.12f,i%2==0?"#FFEE36":"#74F32A",root.transform);}
            root.AddComponent<PurchaseBurst>();PlaySale();
        }
        void OnDestroy() { Destroy(pickup); Destroy(sale); }
    }
    public sealed class PurchaseBurst : MonoBehaviour
    {
        float elapsed;
        void Update(){elapsed+=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*1.8f;transform.localScale=Vector3.one*(1+elapsed*2);if(elapsed>.75f)Destroy(gameObject);}
    }
}
