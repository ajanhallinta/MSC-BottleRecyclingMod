using UnityEngine;
using HutongGames.PlayMaker;
using System.Collections;
using System.Collections.Generic;

namespace BottleRecycling
{
    public class BottleRecyclingManager : MonoBehaviour
    {
        string teimoPath = "STORE/TeimoInShop/Pivot/Teimo/skeleton";
        string teimoHandPath = "STORE/TeimoInShop/Pivot/Teimo/skeleton/pelvis/spine_middle/spine_upper/collar_left/shoulder_left/arm_left/hand_left/ItemPivot";
        string teimoMoneyAnim = "teimo_give_drink";
        string teimoIdleAnim = "teimo_lean_table_in";

        private bool isTeimoTakingBottle = false;

        private Transform player;
        public Transform receiveMoneyTrigger;
        public Transform pricesNote;
        public Transform pricesNoteVisual;

        private float distanceToReceiveMoneyTrigger;
        private float distanceToNote;
        public float totalMoneyAmountFromBottles = 0;

        private AudioSource cash_register_2;
        private AudioSource bottle_empty_3;

        private FsmBool OpenStore;
        private FsmFloat BrokenWindow;
        private FsmString Subtitles;

        string recyclingNoteString = "Bottle deposits: Glass bottle 0,33 (1mk), Empty beer case (14mk), Full beer case (38mk). No goverment's booze bottles!";

        private GameObject ItemPivot;

        public List<string> customBottles = new List<string>();
        public List<int> customBottlePrices = new List<int>();

        private void Start()
        {
            // get player
            player = GameObject.Find("PLAYER").transform;
            // item pivot
            ItemPivot = PlayMakerGlobals.Instance.Variables.GetFsmGameObject("ItemPivot").Value;
            // get audio
            try
            {
                cash_register_2 = GameObject.Find("MasterAudio/Store/cash_register_2").GetComponent<AudioSource>();
                bottle_empty_3 = GameObject.Find("MasterAudio/BottlesEmpty/bottle_empty3").GetComponent<AudioSource>();
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when getting AudioSources.");
            }

            GetStoreFsmVariables(); // OpenStore & BrokenWindow
            Subtitles = PlayMakerGlobals.Instance.Variables.GetFsmString("GUIsubtitle");

        }

        void GetStoreFsmVariables()
        {
            // Store Open Times FSM (OpenStore)
            try
            {
                GameObject STORE = GameObject.Find("STORE");
                foreach (PlayMakerFSM fsm in STORE.GetComponents<PlayMakerFSM>())
                {
                    if (fsm.FsmName == "OpeningHours")
                        OpenStore = fsm.FsmVariables.GetFsmBool("OpenStore");
                }
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when trying to find FsmBool 'OpenStore'");
            }

            // Broken Window Debt FSM (BrokenWindow)
            try
            {
                GameObject Register = GameObject.Find("STORE/StoreCashRegister/Register");
                foreach (PlayMakerFSM fsm in Register.GetComponents<PlayMakerFSM>())
                {
                    if (fsm.FsmName == "Data")
                        BrokenWindow = fsm.FsmVariables.GetFsmFloat("BrokenWindow");
                }
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when trying to find Register FsmFloat 'BrokenWindow'");
            }
        }

        private void Update()
        {
            // is bottle recycling active / can be used
            if (!player || !receiveMoneyTrigger)
                return;

            // get player distance from Receive Money Trigger
            distanceToReceiveMoneyTrigger = Vector3.Distance(player.position, receiveMoneyTrigger.position);
            // get player distance from note
            if (pricesNote != null)
                distanceToNote = Vector3.Distance(player.position, pricesNote.position);

            // update raycast 
            if (distanceToReceiveMoneyTrigger < 1.61f || distanceToNote < 1.15f)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit, 1.61f))
                {
                    // take money trigger
                    if (!isTeimoTakingBottle && totalMoneyAmountFromBottles > 0 && BrokenWindow.Value <= 0 && distanceToReceiveMoneyTrigger < 1.61f && OpenStore.Value)
                    {
                        if (ItemPivot != null)
                        {
                            if (hit.transform.name == "Bottle Receive Money Trigger" && ItemPivot.transform.childCount == 0)
                            //if (hit.transform.name == "TeimoCollider" && ItemPivot.transform.childCount == 0)
                            {
                                // show gui stuff
                                PlayMakerGlobals.Instance.Variables.GetFsmBool("GUIuse").Value = true;
                                PlayMakerGlobals.Instance.Variables.GetFsmString("GUIinteraction").Value = "Take Money " + totalMoneyAmountFromBottles + " MK";
                                // redeem money
                                if (Input.GetMouseButtonDown(0))
                                    GetMoney();
                            }
                        }
                    }

                    // bottle deposits note subtitles
                    if (Subtitles != null)
                        if (hit.transform.name == "Bottle Deposits Note")
                        {
                            Subtitles.Value = recyclingNoteString;
                        }
                        else
                        {
                            if (Subtitles.Value == recyclingNoteString)
                                Subtitles.Value = "";
                        }
                }
            }
            else // Away from Trigger (Raycast inactive)
            {
                if (Subtitles.Value == recyclingNoteString)
                    Subtitles.Value = "";
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.tag != "PART" || other.gameObject.layer == LayerMask.NameToLayer("Wheel") || isTeimoTakingBottle || !OpenStore.Value || BrokenWindow.Value > 0)
                return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb && rb.velocity.magnitude > 0.25f)
                return;

            // empty beer bottle
            if (other.name == "empty bottle(Clone)")
            {
                if (!BottleRecycling.isBoozeBottle(other.transform)) // ignore booze bottles
                    StartCoroutine(TeimoTakesBottle(other.transform)); // start teimo bottle returning sequence
            }

            // empty beercase
            if (other.name == "empty(itemx)")
            {
                // identify gameobject as beercase from fsm id...
                PlayMakerFSM fsm = other.GetComponent<PlayMakerFSM>();
                if (fsm)
                {
                    if (fsm.FsmVariables.FindFsmString("ID").Value.Contains("beercase"))
                    {
                        StartCoroutine(TeimoTakesBottle(other.transform)); // start teimo bottle returning sequence
                        return;
                    }
                }

                // ... if it's not casual beercase, check if it has been filled with empty bottles.
                BeercaseFilled filled = other.GetComponent<BeercaseFilled>();
                if (filled)
                {
                    StartCoroutine(TeimoTakesBottle(other.transform)); // start teimo bottle returning sequence
                }
            }

            // last we check for possible custom bottles
            if (customBottles.Count > 0)
            {
                if (customBottles.Contains(other.name))
                {
                    int index = customBottles.IndexOf(other.name);
                    if (index != -1)
                    {
                        StartCoroutine(TeimoTakesBottle(other.transform));
                    }
                }
            }
        }

        // Succesful Bottle Returning Sequence Logic
        IEnumerator TeimoTakesBottle(Transform bottleTransform)
        {
            isTeimoTakingBottle = true;
            bottleTransform.tag = "Untagged"; // prevent player from grabbing the given bottle back

            try
            {
                // play Teimo's "give drink" animation backwards
                Animation teimoAnimation = GameObject.Find(teimoPath).GetComponent<Animation>();
                teimoAnimation[teimoMoneyAnim].speed = -teimoAnimation[teimoMoneyAnim].speed;
                teimoAnimation[teimoMoneyAnim].time = teimoAnimation[teimoMoneyAnim].length;
                teimoAnimation.CrossFade(teimoMoneyAnim, 0.25f);

                // disable given bottle collision and attach it to Teimo's hand
                Rigidbody rb = bottleTransform.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                }
                bottleTransform.parent = GameObject.Find(teimoHandPath).transform;
                bottleTransform.localPosition = Vector3.zero;
                bottleTransform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            catch
            {
                BottleRecycling.DebugPrint("Error on bottle returning sequence.");
            }

            yield return new WaitForSeconds(2.5f); // give time for Teimo to put bottle under desk
            AddMoneyToHold(GetBottleValue(bottleTransform));
            Destroy(bottleTransform.gameObject); // destroy the bottle
            PlaySound(bottle_empty_3);
            yield return new WaitForSeconds(3.5f); // wait for animation to approximately complete

            try
            {
                // restore Teimo's animation speed
                Animation teimoAnimation = GameObject.Find(teimoPath).GetComponent<Animation>();
                teimoAnimation[teimoMoneyAnim].speed = Mathf.Abs(teimoAnimation[teimoMoneyAnim].speed);
              
                try
                {
                    // restore Teimo's idle animation
                    teimoAnimation.Play(teimoIdleAnim);
                    teimoAnimation[teimoIdleAnim].time = teimoAnimation[teimoIdleAnim].length;
                } catch { }
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when restoring Teimo's animation speed.");
            }
            isTeimoTakingBottle = false; // bottle returning sequence is over
        }

        void GetMoney()
        {
            PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerMoney").Value += totalMoneyAmountFromBottles;
            try
            {
                GameObject.Find(teimoPath).GetComponent<Animation>().Play("teimo_cash_register");
                PlaySound(cash_register_2);
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when playing Teimo's animation.");
            }
            totalMoneyAmountFromBottles = 0;
        }

        void AddMoneyToHold(float amount)
        {
            totalMoneyAmountFromBottles += amount;
        }

        float GetBottleValue(Transform bottleTransform)
        {
            if (bottleTransform == null)
                return 0;

            switch (bottleTransform.name)
            {
                case "empty bottle(Clone)": // empty bottle
                    return 1;
                case "empty(itemx)": // empty beercase
                    // filled case
                    BeercaseFilled filled = bottleTransform.GetComponent<BeercaseFilled>();
                    if (filled != null)
                    {
                        int totalValue = 14 + (filled.totalBottles * 1);
                        return totalValue; 
                    }
                    // casual case
                    return 14;
                default:
                    // custom bottles
                    if(customBottles.Count>0)
                    {
                        if(customBottles.Contains(bottleTransform.name))
                        {
                            int index = customBottles.IndexOf(bottleTransform.name);
                            if(index <= customBottlePrices.Count)
                            {
                                return customBottlePrices[index];
                            }
                        }
                    }
                    // fail-safe
                    return 1;
            }
        }
        void PlaySound(AudioSource audio)
        {
            if (audio == null)
            {
                BottleRecycling.DebugPrint("Error; Tried to play null AudioSource.");
                return;
            }

            try
            {
                if (audio == bottle_empty_3)
                    audio.transform.position = new Vector3(-1550.986f, 4.370f, 1183.482f);
                else if (audio == cash_register_2)
                    audio.transform.position = new Vector3(-1551.547f, 4.962f, 1182.302f);
                audio.Play();
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when playing AudioSource.");
            }
        }
    }
}
