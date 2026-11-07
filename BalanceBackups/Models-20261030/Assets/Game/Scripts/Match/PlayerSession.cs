using System;
using UnityEngine;
using Bellator.Classes;

namespace Bellator.Match
{
    /// <summary>Session identity is issued only after credential validation, and never taken from UI text.</summary>
    public static class PlayerSession
    {
        static LocalAccountStore accounts;
        static string sessionToken;
        public static LocalAccountStore Accounts => accounts??(accounts=new LocalAccountStore());
        public static bool HasSession { get; private set; }
        public static LocalPlayerProfile Profile { get; private set; }
        public static bool IsAccount => HasSession && Profile!=null;
        public static string Username => IsAccount?Profile.Username:"Convidado";
        public static event Action Changed;
        internal static string Token => sessionToken;
        static bool CanChange => UnityEngine.Object.FindAnyObjectByType<GameManager>()==null;
        public static bool EnterAccount(AccountResult result)
        {
            if(!CanChange || result==null || !result.Success) return false;
            Set(result.Profile); return true;
        }
        public static bool EnterGuest()
        { if(!CanChange) return false; Set(null); return true; }
        public static bool Logout()
        {
            if(!CanChange) return false;
            Profile=null; HasSession=false; sessionToken=null; ClassSelectionState.Confirm(null); GameModeManager.Select(GameMode.Story); Changed?.Invoke(); return true;
        }
        static void Set(LocalPlayerProfile profile)
        {
            Profile=profile; HasSession=true; sessionToken=Guid.NewGuid().ToString("N");
            ClassSelectionState.Confirm(null); GameModeManager.Select(GameMode.Story); Changed?.Invoke();
        }
        internal static bool OwnsRun(string profileId,string token) => IsAccount && Profile.Id==profileId && sessionToken==token && !string.IsNullOrEmpty(token);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { accounts=null;Profile=null;HasSession=false;sessionToken=null;Changed=null; }
    }
}
