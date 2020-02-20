using MSCLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace BottleRecycling
{
    public class BeercaseFilled : MonoBehaviour
    {
        public int totalBottles = 0;

        public void AddBottleToBeerCase(GameObject emptyBeerBottle)
        {
            Vector3 bottleLocalPosition = BeercaseManager.GetEmptyBottlePositionForBeercase(totalBottles);
            if (bottleLocalPosition == Vector3.zero)
                return;

            emptyBeerBottle.SetActive(true);

            if (emptyBeerBottle.GetComponent<Rigidbody>() != null)
            {
                emptyBeerBottle.GetComponent<Rigidbody>().isKinematic = true;
                emptyBeerBottle.GetComponent<Rigidbody>().detectCollisions = false;
                emptyBeerBottle.GetComponent<Rigidbody>().useGravity = false;
                GameObject.Destroy(emptyBeerBottle.GetComponent<Rigidbody>());
            }
            if (emptyBeerBottle.GetComponent<Collider>() != null)
            {
                emptyBeerBottle.GetComponent<Collider>().enabled = false;
                GameObject.Destroy(emptyBeerBottle.GetComponent<Collider>());
            }
              

            emptyBeerBottle.tag = "Untagged";
            emptyBeerBottle.layer = LayerMask.NameToLayer("Default");
            emptyBeerBottle.name = "empty_bottle";

            emptyBeerBottle.transform.parent = transform;
            emptyBeerBottle.transform.localPosition = bottleLocalPosition;
            emptyBeerBottle.transform.localEulerAngles = Vector3.zero;

            totalBottles++;
            StartCoroutine(ForceBottlesToStayInBeercase(emptyBeerBottle, gameObject)); // fix for bottle parentness
        }

        // HACK/FIX: beer bottle's parentness is nulled if we don't set parentness again in next frame.
        IEnumerator ForceBottlesToStayInBeercase(GameObject beerBottle, GameObject beercase)
        {
            beerBottle.transform.parent = beercase.transform;
            yield return new WaitForEndOfFrame();
            beerBottle.transform.parent = beercase.transform;
        }
    }
}
