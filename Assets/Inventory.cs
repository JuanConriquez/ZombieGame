using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private HashSet<string> keys = new HashSet<string>(); // Keys created via HashSet so each key is unique

    // AddKey(string keyID)
    // Function for when the player obtains a key to be added into the Inventory
    public void AddKey(string keyID)
    {
        if (!string.IsNullOrEmpty(keyID)) // When a key is collected
        {
            keys.Add(keyID); //Add to Inventory
            Debug.Log($"[Inventory] Added Key: {keyID}");
        }
    }
    
   //lowkey not used at all bc we just have on door that needs all keys
    public bool HasKey(string keyID)
    {
        if (string.IsNullOrEmpty(keyID)) return false; //If incorrect key is used for door
        return keys.Contains(keyID); //Otherwise, use key to open door
    }


    public bool HasAllKeys(IEnumerable<string> keyIDs)
    {
        if (keyIDs == null) return false;

        foreach (string id in keyIDs)
        {
            if (string.IsNullOrEmpty(id)) return false;   
            if (!keys.Contains(id)) return false;         // missing one of the keys
        }

        return true; //only if all 4 keys collected
    }
}
