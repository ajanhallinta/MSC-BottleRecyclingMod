﻿using UnityEngine;
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
            
            // Get and destroy bottle rigidbody
            Rigidbody emptyBeerBottleRigidbody = emptyBeerBottle.GetComponent<Rigidbody>();
            if (emptyBeerBottleRigidbody != null)
                GameObject.Destroy(emptyBeerBottleRigidbody);

            // Get and destroy bottle collider
            Collider emptyBeerBottleCollider = emptyBeerBottle.GetComponent<Collider>();
            if (emptyBeerBottleCollider != null)
                GameObject.Destroy(emptyBeerBottleCollider);
              

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
