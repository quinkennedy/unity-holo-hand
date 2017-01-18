using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InstalledPackagesResponse {
    public Package[] InstalledPackages;

    [System.Serializable]
    public class Package
    {
        public bool CanUninstall;
        public string Name;
        public string PackageFamilyName;
        public string PackageFullName;
        public int PackageOrigin;
        public string PackageRelativeId;
        public string Publisher;
        public VersionDetails Version;
        public User[] RegisteredUsers;

        [System.Serializable]
        public class VersionDetails
        {
            public int Build;
            public int Major;
            public int Minor;
            public int Revision;
        }

        [System.Serializable]
        public class User
        {
            public string UserDisplayName;
            public string UserSID;
        }
    }
    
    public static InstalledPackagesResponse CreateFromJSON(string jsonString)
    {
        return JsonUtility.FromJson<InstalledPackagesResponse>(jsonString);
    }
}
