using System;
using UnityEngine;

namespace Bellator.Combat
{
    public sealed class WeaponEquipment : MonoBehaviour
    {
        [SerializeField] private Transform rightHandSocket,leftHandSocket;
        [SerializeField] private WeaponDefinition[] loadout;
        [SerializeField] private WeaponDefinition shieldDefinition;
        private GameObject weaponVisual,shieldVisual;
        private bool shieldRequested=true;
        private bool fixedEquipment;
        public bool IsFixed => fixedEquipment;
        public event Action Changed;
        public WeaponDefinition Current { get; private set; }
        public WeaponDefinition Shield => HasShield ? shieldDefinition : null;
        public bool HasShield => Current!=null && Current.permitsShield && shieldRequested && shieldDefinition!=null;
        public int Slot { get; private set; }
        public Transform Muzzle { get; private set; }
        public Transform ShieldVisual => shieldVisual!=null ? shieldVisual.transform : null;
        public WeaponDefinition[] Loadout => loadout;
        public void Configure(Transform right,Transform left,WeaponDefinition[] weapons,WeaponDefinition shield)
        { rightHandSocket=right; leftHandSocket=left; loadout=weapons; shieldDefinition=shield; shieldRequested=true; fixedEquipment=false; }
        // Class equipment is initialized once; runtime slot/shield calls cannot replace it.
        public void ConfigureFixed(Transform right,Transform left,WeaponDefinition weapon,WeaponDefinition shield)
        { Configure(right,left,new[]{weapon},shield); Equip(0); fixedEquipment=true; }
        private void Start() => Equip(0);
        public bool Equip(int slot)
        {
            if(fixedEquipment) return slot==0 && Current==loadout[0];
            if(loadout==null || slot<0 || slot>=loadout.Length || loadout[slot]==null) return false;
            Clear(ref weaponVisual); Clear(ref shieldVisual);
            Current=loadout[slot]; Slot=slot;
            if(Current.visualPrefab!=null)
            {
                weaponVisual=Instantiate(Current.visualPrefab,Current.leftHandMount?leftHandSocket:rightHandSocket,false);
                weaponVisual.transform.localPosition=Current.mountOffset;
                weaponVisual.transform.localRotation=Quaternion.Euler(Current.mountEuler);
                Muzzle=weaponVisual.transform.Find("Muzzle");
            }
            else Muzzle=null;
            RefreshShield(); Changed?.Invoke(); return true;
        }
        public void ToggleShield() { if(fixedEquipment) return; shieldRequested=!shieldRequested; RefreshShield(); Changed?.Invoke(); }
        private void RefreshShield()
        {
            Clear(ref shieldVisual);
            if(HasShield && shieldDefinition.visualPrefab!=null)
            {
                shieldVisual=Instantiate(shieldDefinition.visualPrefab,leftHandSocket,false);
                shieldVisual.transform.localPosition=shieldDefinition.mountOffset;
                shieldVisual.transform.localRotation=Quaternion.Euler(shieldDefinition.mountEuler);
            }
        }
        private static void Clear(ref GameObject visual)
        { if(visual!=null) { visual.SetActive(false); Destroy(visual); } visual=null; }
    }
}




