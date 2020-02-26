using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace BottleRecycling
{
    public class BeercaseManager : MonoBehaviour
    {
        public  GameObject ItemPivot;
        private AudioSource bottle_empty_2;

        public static List<Vector3> bottlePositions = new List<Vector3>()
        {
            new Vector3(-0.032094002f, -0.095451124f, -0.016192561f), // 1
            new Vector3(0.03223896f, 0.033216726f, -0.016192516f), // 2
            new Vector3(0.16168118f, 0.033216488f, -0.016192516f), // 3
            new Vector3(0.096572399f, 0.033216726f, -0.016192516f), // 4
            new Vector3(-0.096426964f, -0.095451124f, -0.016192561f), // 5
            new Vector3(-0.032094002f, 0.033216726f, -0.016192516f), // 6
            new Vector3(-0.15843487f, -0.095451124f, -0.016192561f), // 7
            new Vector3(0.16168118f, 0.095225111f, -0.016192494f), // 8
            new Vector3(0.096572399f, 0.095224872f, -0.016192494f), // 9
            new Vector3(-0.15843487f, -0.031892296f, -0.016192539f), // 10
            new Vector3(0.096572399f, -0.031892296f, -0.016192539f), // 11
            new Vector3(0.16168118f, -0.031892534f, -0.016192539f), // 12
            new Vector3(-0.096426964f, 0.033216726f, -0.016192516f), // 13
            new Vector3(-0.032094002f, 0.095224872f, -0.016192494f), // 14
            new Vector3(0.16168165f, -0.095451124f, -0.016192948f), // 15
            new Vector3(0.03223896f, -0.031892296f, -0.016192539f), // 16
            new Vector3(0.03223896f, -0.095451124f, -0.016192561f), // 17
            new Vector3(-0.15843487f, 0.033216726f, -0.016192516f), // 18
            new Vector3(-0.096426964f, -0.031892296f, -0.016192539f), // 19
            new Vector3(-0.15843487f, 0.095224872f, -0.016192494f), // 20
            new Vector3(0.096572399f, -0.095451124f, -0.016192561f), // 21
            new Vector3(0.03223896f, 0.095224872f, -0.016192494f), // 22
            new Vector3(-0.032094002f, -0.031892296f, -0.016192539f), // 23
            new Vector3(-0.096426964f, 0.095224872f, -0.016192494f) // 24
        };

        private void Start()
        {
            ItemPivot = PlayMakerGlobals.Instance.Variables.GetFsmGameObject("ItemPivot").Value;
            try
            {
                bottle_empty_2 = GameObject.Find("MasterAudio/BottlesEmpty/bottle_empty2").GetComponent<AudioSource>();
            }
            catch
            {
                BottleRecycling.DebugPrint("Error when trying to get bottle_empty2 sound.");
            }
        }

        private void Update()
        {
            if (ItemPivot == null)
                return;

            // update BeercaseManager Trigger collider position to GameObject in hand position
            if (ItemPivot.transform.childCount > 0)
            {
                // if empty bottle is in player hands, put this object in same position
                if (ItemPivot.transform.GetChild(0).name == "empty bottle(Clone)")
                {
                    transform.position = ItemPivot.transform.GetChild(0).position;
                }
            }
            else
            {
                transform.position = new Vector3(10000, 10000, 10000); // put trigger out-of-bounds when no object in hands.
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (ItemPivot == null)
                return;
            if (ItemPivot.transform.childCount == 0)
                return;

            if (other.name == "empty(itemx)" && isBeercase(other))
            {
                if (BottleRecycling.isBoozeBottle(ItemPivot.transform.GetChild(0)))
                    return;

                // show gui stuff
                PlayMakerGlobals.Instance.Variables.GetFsmBool("GUIuse").Value = true;
                PlayMakerGlobals.Instance.Variables.GetFsmString("GUIinteraction").Value = "Put bottle to beer case";
                if (Input.GetMouseButtonDown(0))
                {
                    PutBottleToBeercase(ItemPivot.transform.GetChild(0).gameObject, other.gameObject);
                }
            }
        }

        bool isBeercase(Collider other)
        {
            try
            {
                // identify collider as empty beercase from fsm ID value...
                return other.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmString("ID").Value.Contains("beercase");
            } catch { }

            return other.GetComponent<BeercaseFilled>(); // ... or if beercase has already empty bottles in.
        }

        void PutBottleToBeercase(GameObject bottle, GameObject beercase)
        {
            if (bottle == null || beercase == null)
            {
                BottleRecycling.DebugPrint("BeercaseManager: Error when trying to put empty bottle to empty beercase.");
                return;
            }

            // Add BeercaseFilled -component
            if (beercase.GetComponent<BeercaseFilled>() == null)
                beercase.AddComponent<BeercaseFilled>();
            try
            {
                Destroy(beercase.GetComponent<PlayMakerFSM>()); // Remove PlayMakerFSM from Beercase
            }
            catch { }

            BeercaseFilled beercaseFilled = beercase.GetComponent<BeercaseFilled>();

            // Only 24 bottles fits to beercase (duh)
            if (beercaseFilled.totalBottles >= 24)
                return;

            beercaseFilled.AddBottleToBeerCase(bottle); // Add Empty Bottle to Beercase
            PlaySound(bottle_empty_2, beercase.transform.position); // Play Sound
        }

        public static Vector3 GetEmptyBottlePositionForBeercase(int id)
        {
            return id > bottlePositions.Count ? Vector3.zero : bottlePositions[id];
        }

        public void PlaySound(AudioSource audio, Vector3 position)
        {
            if (audio == null)
                return;
            audio.transform.position = position;
            audio.Play();
        }
    }
}
