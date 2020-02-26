using MSCLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace BottleRecycling
{
    public class SaveData
    {
        public float unredeemedBottleDeposit;
        public List<SaveDataEmptyBottle> emptyBottles = new List<SaveDataEmptyBottle>();
        public List<SaveDataFilledBeercase> filledBeercases = new List<SaveDataFilledBeercase>();
    }

    public class SaveDataEmptyBottle
    {
        public string Name = "";
        public Vector3 Position;
        public float RotX, RotY, RotZ;
    }

    public class SaveDataFilledBeercase
    {
        public string Name = "";
        public Vector3 Position;
        public float RotX, RotY, RotZ;
        public int bottleAmount;
    }

    public class BottleRecycling : Mod
    {
        public override string ID => "BottleRecycling"; //My mod ID (unique)
        public override string Name => "Bottle Recycling"; //My mod name
        public override string Author => "ajanhallinta"; //My Username
        public override string Version => "1.03"; //Version

        // Set this to true if you will be load custom assets from Assets folder.
        // This will create subfolder in Assets folder for your mod.
        public override bool UseAssetsFolder => true;

        // Managers
        private BottleRecyclingManager bottleRecyclingManager;
        private BeercaseManager beerCaseManager;

        // Settings
        Settings useFilledBeercases = new Settings("useFilledBeercases", "Beercase filling with empty bottles", true);
        Settings useAdditionalStoreGfx = new Settings("useAdditionalStoreGfx", "Additional store graphics", true);
        Settings useCustomBottles = new Settings("useCustomBottles", "Use custom_bottles.txt", false);
        Settings saveEmptyBottles = new Settings("saveEmptyBottles", "Save/Load empty bottles", true);
        Settings saveFilledBeercases= new Settings("saveFilledBeercases", "Save/Load filled beercases", true);
        Settings printStats = new Settings("printStats", "Print statistics to console OnLoad", false);
        Settings clearEmptyBottles = new Settings("clearEmptyBottles", "Destroy empty bottles from map", DestroyEmptyBottles);

        // Prefabs
        public static GameObject emptyBottlePrefab;
        public static GameObject filledBeercasePrefab;

        AssetBundle ab;

        public override void ModSettings()
        {
            // All settings should be created here. 
            // DO NOT put anything else here that settings.
            Settings.AddCheckBox(this, saveEmptyBottles);
            Settings.AddCheckBox(this, saveFilledBeercases);
            Settings.AddCheckBox(this, useFilledBeercases);
            Settings.AddCheckBox(this, useAdditionalStoreGfx);
            Settings.AddCheckBox(this, useCustomBottles);
            Settings.AddCheckBox(this, printStats);
            Settings.AddButton(this, clearEmptyBottles);
            Settings.AddHeader(this, "About");
            Settings.AddText(this, "Made by: ajanhallinta");
            Settings.AddText(this, "Thanks to: Toplessgun, piotrulos, Athlon007, zamp, eps, haverdaven (DD), Keippa & Zeron");
            Settings.AddText(this, "Save me from collecting bottles: https://www.paypal.me/ajanhallinta");
        }

        public override void OnNewGame()
        {
            // Called once, when starting a New Game, you can reset your saves here
            CreateFreshSaveFile();
        }

        public override void OnLoad() // Called once, when mod is loading after game is fully loaded
        {
            // Create BottleRecyclingManager / Desk Trigger
            GameObject bottleRecyclingTrigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bottleRecyclingTrigger.name = "Bottle Recycling Manager";
            bottleRecyclingTrigger.transform.position = new Vector3(-1551.096f, 4.712f, 1182.784f);
            bottleRecyclingTrigger.transform.localEulerAngles = new Vector3(-90, 0, 61.256f);
            bottleRecyclingTrigger.transform.localScale = new Vector3(0.4274191f, 0.5956179f, 0.1249701f);
            bottleRecyclingTrigger.GetComponent<Collider>().isTrigger = true;
            bottleRecyclingManager = bottleRecyclingTrigger.AddComponent<BottleRecyclingManager>();
            GameObject.Destroy(bottleRecyclingTrigger.GetComponent<MeshRenderer>());
            GameObject.Destroy(bottleRecyclingTrigger.GetComponent<MeshFilter>());

            // Create Receive Money Trigger           
            GameObject receiveMoneyTrigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiveMoneyTrigger.name = "Bottle Receive Money Trigger";
            receiveMoneyTrigger.layer = LayerMask.NameToLayer("DontCollide");
            receiveMoneyTrigger.transform.position = new Vector3(-1551.336f, 5.0255f, 1183.061f);
            receiveMoneyTrigger.transform.localEulerAngles = new Vector3(-90, 0, 57.398f);
            receiveMoneyTrigger.transform.localScale = new Vector3(0.2007769f, 0.5619151f, 0.8257601f);
            receiveMoneyTrigger.GetComponent<Collider>().isTrigger = true;
            GameObject.Destroy(receiveMoneyTrigger.GetComponent<MeshRenderer>());
            GameObject.Destroy(receiveMoneyTrigger.GetComponent<MeshFilter>());
            bottleRecyclingManager.receiveMoneyTrigger = receiveMoneyTrigger.transform;

            // Create Recycling Prices Note
            if ((bool)useAdditionalStoreGfx.GetValue())
            {
                GameObject bottleDepositsNoteVisual = GameObject.CreatePrimitive(PrimitiveType.Plane);
                GameObject.Destroy(bottleDepositsNoteVisual.GetComponent<Collider>());
                bottleDepositsNoteVisual.transform.position = new Vector3(-1551.484f, 4.36f, 1182.2f);
                bottleDepositsNoteVisual.transform.localEulerAngles = new Vector3(270, 327.354f, 0);
                bottleDepositsNoteVisual.transform.localScale = new Vector3(0.023f, 0.02f, 0.032f);
                bottleDepositsNoteVisual.name = "Bottle Deposits Note Visual";
                bottleRecyclingManager.pricesNoteVisual = bottleDepositsNoteVisual.transform;

                GameObject bottleDepositsNote = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bottleDepositsNote.name = "Bottle Deposits Note";
                bottleDepositsNote.layer = LayerMask.NameToLayer("DontCollide");
                bottleDepositsNote.transform.position = new Vector3(-1551.484f, 4.36f, 1182.2f);
                bottleDepositsNote.transform.localEulerAngles = new Vector3(0, -32.646f, 0);
                bottleDepositsNote.transform.localScale = new Vector3(0.1900704f, 0.2755774f, 0.001717138f);
                bottleDepositsNote.GetComponent<Collider>().isTrigger = true;
                bottleDepositsNote.GetComponent<MeshRenderer>().enabled = false;
                bottleRecyclingManager.pricesNote = bottleDepositsNote.transform;
            }

            // Create BeercaseManager
            if ((bool)useFilledBeercases.GetValue())
            {
                GameObject _beerCaseManager = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beerCaseManager = _beerCaseManager.AddComponent<BeercaseManager>();
                beerCaseManager.name = "Beercase Manager";
                beerCaseManager.GetComponent<Collider>().isTrigger = true;
                beerCaseManager.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                GameObject.Destroy(beerCaseManager.GetComponent<MeshRenderer>());
                GameObject.Destroy(beerCaseManager.GetComponent<MeshFilter>());
            }

            // Custom Bottles
            if ((bool)useCustomBottles.GetValue())
            {
                string[] customBottles = null;
                try
                {
                    customBottles = System.IO.File.ReadAllLines(System.IO.Path.Combine(ModLoader.GetModConfigFolder(this), "custom_bottles.txt"));
                }
                catch { }

                if (customBottles != null)
                {
                    foreach (string s in customBottles)
                    {
                        if (s.StartsWith("//") || string.IsNullOrEmpty(s)) // ignore comments
                            continue;

                        string[] chars = s.Split('=');
                        if (chars.Length > 1)
                        {
                            if (int.TryParse(chars[1], out int _price))
                            {
                                bottleRecyclingManager.customBottles.Add(chars[0]);
                                bottleRecyclingManager.customBottlePrices.Add(_price);
                            }
                        }
                    }
                }
            }
      
            ab = LoadAssets.LoadBundle(this, "bottlerecyclingbundle"); // load assetbundle
            LoadSaveFile(); // load savefile

            // apply bottle deposits note material
            if (bottleRecyclingManager.pricesNote != null)
            {
                Material noteMaterial = ab.LoadAsset("bottleDepositNoteMat") as Material;
                try
                {
                    bottleRecyclingManager.pricesNoteVisual.GetComponent<MeshRenderer>().material = GameObject.Instantiate(noteMaterial) as Material;
                }
                catch { }
            }

            ab.Unload(false); // unload assetbundle
        }

        void LoadSaveFile()
        {
            // Load Savefile
            try
            {
                SaveData data = SaveLoad.DeserializeSaveFile<SaveData>(this, "BottleRecyclingSave.save");
                if (data != null)
                {
                    // Get Unredeemed Bottle Deposits
                    bottleRecyclingManager.totalMoneyAmountFromBottles = data.unredeemedBottleDeposit;

                    // Create Empty Bottle Prefab
                    if((bool)saveEmptyBottles.GetValue() || (bool)saveFilledBeercases.GetValue())
                    {
                        GameObject bottlePrefab = ab.LoadAsset("EmptyBeerBottle") as GameObject;
                        bottlePrefab.tag = "PART";
                        bottlePrefab.layer = LayerMask.NameToLayer("Parts");
                        emptyBottlePrefab = GameObject.Instantiate(bottlePrefab) as GameObject;
                        emptyBottlePrefab.SetActive(false);          
                    }

                    // Spawn Saved Bottles
                    if ((bool)saveEmptyBottles.GetValue() && data.emptyBottles.Count > 0)
                    {                    
                        // Spawn Empty Bottles
                        foreach (SaveDataEmptyBottle savedBottle in data.emptyBottles)
                        {
                            GameObject newBottle = GameObject.Instantiate(emptyBottlePrefab) as GameObject;
                            newBottle.transform.position = savedBottle.Position;
                            newBottle.transform.eulerAngles = new Vector3(savedBottle.RotX, savedBottle.RotY, savedBottle.RotZ);
                            newBottle.name = savedBottle.Name;
                            newBottle.SetActive(true);
                        }
                    }

                    // Spawn Filled Beercases
                    if (data.filledBeercases.Count > 0 && (bool)useFilledBeercases.GetValue())
                    {
                        beerCaseManager.StartCoroutine(SpawnSavedFilledBeercases(data));
                    }

                    // Print Statistic to Console
                    if ((bool)printStats.GetValue())
                    {
                        DebugPrint("Savefile statistics: unredeemed bottle deposits (" + data.unredeemedBottleDeposit + " MK), saved empty bottles (" + data.emptyBottles.Count + "), saved beercases (" + data.filledBeercases.Count + ").");
                    }
                }
            }
            catch
            {
                DebugPrint("Error when loading savefile!");
            }
        }

        public void CreateFreshSaveFile()
        {
            SaveData sd = new SaveData();
            SaveLoad.SerializeSaveFile(this, sd, "BottleRecyclingSave.save");
            if((bool)printStats.GetValue())
                DebugPrint("Created fresh savefile.");
        }

        IEnumerator SpawnSavedFilledBeercases(SaveData saveData)
        {
            yield return new WaitForEndOfFrame(); // wait one frame for apparently no reason

            // Create Empty Beercase prefab
            filledBeercasePrefab = GetEmptyBeercase(); 
            filledBeercasePrefab.SetActive(true);
            if (filledBeercasePrefab.GetComponent<PlayMakerFSM>())
                GameObject.Destroy(filledBeercasePrefab.GetComponent<PlayMakerFSM>()); // Destroy FSM Component from clone.

            yield return new WaitForEndOfFrame(); // wait one frame after destroying fsm

            // Destroy all existing child bottles from Beercase prefab.
            foreach (Transform child in filledBeercasePrefab.GetComponentsInChildren<Transform>())
            {
                if (child != filledBeercasePrefab.transform)
                    GameObject.Destroy(child.gameObject);
            }

            yield return new WaitForEndOfFrame(); // wait one frame before creating empty bottles to case.

            if(saveData.filledBeercases.Count>0)
            {
                foreach(SaveDataFilledBeercase filledBeercase in saveData.filledBeercases)
                {
                    // Create new Beercase
                    GameObject newCase = GameObject.Instantiate(filledBeercasePrefab) as GameObject;
                    newCase.transform.position = filledBeercase.Position;
                    newCase.transform.eulerAngles = new Vector3(filledBeercase.RotX, filledBeercase.RotY, filledBeercase.RotZ);
                    newCase.name = "empty(itemx)";

                    // Make Beercase Fillable
                    BeercaseFilled filled = newCase.AddComponent<BeercaseFilled>();
                    // Add Empty Bottles GameObjects to New Beercase
                    for (int i = 0; i < filledBeercase.bottleAmount; i++)
                    {
                        filled.AddBottleToBeerCase(GameObject.Instantiate(emptyBottlePrefab) as GameObject);
                    }
                }
            }

            // Empty Beercase prefab is not needed anymore; destroy it.
            GameObject.Destroy(filledBeercasePrefab);

            yield return null;
        }

        public override void OnSave()
        {
            // Called once, when save and quit
            // Serialize your save file here.
            SaveData sd = new SaveData();
            sd.unredeemedBottleDeposit = bottleRecyclingManager.totalMoneyAmountFromBottles;

            // Save Empty Bottles and Beercases, if set so.
            if ((bool)saveEmptyBottles.GetValue() || (bool)saveFilledBeercases.GetValue())
                foreach (GameObject go in GameObject.FindGameObjectsWithTag("PART"))
                {
                    // empty bottles
                    if (go.name == "empty bottle(Clone)" && (bool)saveEmptyBottles.GetValue())
                    {
                        if (isBoozeBottle(go.transform)) // ignore booze bottles
                            continue;
                        SaveDataEmptyBottle emptyBottle = CreateSaveDataEmptyBottle(go);
                        sd.emptyBottles.Add(emptyBottle);
                        continue;
                    }

                    // filled beercases
                    if (go.name == "empty(itemx)" && (bool)saveFilledBeercases.GetValue())
                    {
                        BeercaseFilled filled = go.GetComponent<BeercaseFilled>();
                        if (filled)
                        {
                            SaveDataFilledBeercase filledBeercase = CreateSaveDataFilledBeercase(go);
                            sd.filledBeercases.Add(filledBeercase);
                            continue;
                        }
                    }
                }

            // save to file
            SaveLoad.SerializeSaveFile(this, sd, "BottleRecyclingSave.save");
        }

        SaveDataEmptyBottle CreateSaveDataEmptyBottle(GameObject go)
        {
            SaveDataEmptyBottle emptyBottle = new SaveDataEmptyBottle();
            emptyBottle.Name = go.name;
            emptyBottle.Position = go.transform.position;
            emptyBottle.RotX = go.transform.rotation.eulerAngles.x;
            emptyBottle.RotY = go.transform.rotation.eulerAngles.y;
            emptyBottle.RotZ = go.transform.rotation.eulerAngles.y;
            return emptyBottle;
        }

        SaveDataFilledBeercase CreateSaveDataFilledBeercase(GameObject go)
        {
            SaveDataFilledBeercase filledBeercase = new SaveDataFilledBeercase();
            filledBeercase.Name = go.name;
            filledBeercase.Position = go.transform.position;
            filledBeercase.RotX = go.transform.rotation.eulerAngles.x;
            filledBeercase.RotY = go.transform.rotation.eulerAngles.y;
            filledBeercase.RotZ = go.transform.rotation.eulerAngles.z;
            filledBeercase.bottleAmount = go.GetComponent<BeercaseFilled>().totalBottles;
            return filledBeercase;
        }

        public static void DebugPrint(string message)
        {
            MSCLoader.ModConsole.Print("BottleRecyclingMod: " + message);
        }

        GameObject GetEmptyBeercase()
        {

            // Method 1
            try
            {
                GameObject newCase = GameObject.Instantiate(GameObject.Find("ITEMS/empty(itemx)") as GameObject);
                if (newCase != null)
                {
                    return newCase;
                }
                 
            } catch
            {
                GameObject newCase = GameObject.Instantiate(GameObject.Find("ITEMS/beer case(itemx)") as GameObject);
                if (newCase != null)
                {
                    return newCase;
                }
                  
            }

            // Method 2 (From World)
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("PART"))
            {
                if (go.name == "beer case(itemx)")
                {
                    foreach (PlayMakerFSM fsm in go.GetComponents<PlayMakerFSM>())
                    {
                        if (fsm.FsmName == "Use")
                        {
                            if (fsm.FsmVariables.FindFsmString("ID").Value.Contains("beercase"))
                            {
                                GameObject newCase = GameObject.Instantiate(go) as GameObject;
                                return newCase;
                            }
                        }
                    }
                }
            }

            DebugPrint("Error: Can't find Beercase GameObject to Instantiate!");
            return null;
        }

        public static void DestroyEmptyBottles()
        {
            if(!GameObject.Find("PLAYER"))
            {
                DebugPrint("Empty bottles can be only destroyed when in map!");
                return;
            }

            int destroyedBottles = 0;
            foreach(GameObject go in GameObject.FindGameObjectsWithTag("PART"))
            {
                if(go.name=="empty bottle(Clone)")
                {
                    if (!isBoozeBottle(go.transform))
                    {
                        destroyedBottles++;
                        GameObject.Destroy(go);
                    }
                }
            }

            DebugPrint("Destroyed total " + destroyedBottles + " empty bottles.");
        }

        public static bool isBoozeBottle(Transform other)
        {
            try
            {
                return other.GetComponent<MeshFilter>().sharedMesh.name.Contains("booze_bottle_hand");
            }
            catch
            {
                return false;
            }
        }
    }
}
