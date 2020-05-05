﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScannerIntersectionManager : MonoBehaviour
{
    [SerializeField] private BoxCollider checker;
    [SerializeField] private SphereCollider swordRadius;

    public void CheckIntersections()
    {
        DeleteIntersections();
        float checkerRadius = checker.size.x/2;
        int circumferencesToCheck = (int)(2 * Mathf.PI * swordRadius.radius / (checkerRadius * 2));
        float angleY = 0, angleX = 0;

        for (int i = 0; i < circumferencesToCheck/2; i++)
        {
            for (int r = 0; r < circumferencesToCheck; r++)
            {
                Vector3 centerPointToOverlap = swordRadius.transform.position + Quaternion.Euler(angleX, angleY, 0) * swordRadius.transform.forward * swordRadius.radius;

                Collider[] overlapCols = Physics.OverlapSphere(centerPointToOverlap, checker.size.x / 8);

                for (int c = 0; c < overlapCols.Length; c++)
                {
                    if ((overlapCols[c].gameObject.layer == 13 || overlapCols[c].gameObject.layer == 14) && overlapCols[c].isTrigger == false)
                    {
                        GameObject g = ObjectPooler.SharedInstance.GetPooledObject();
                        g.transform.position = centerPointToOverlap;
                        g.transform.forward = swordRadius.transform.position - g.transform.position;
                        g.SetActive(true);
                        break;
                    }
                }
                angleX += 360.0f / circumferencesToCheck;
            }
            angleY += 360.0f / circumferencesToCheck;
            angleX = 0;
        }
    }

    public void DeleteIntersections()
    {
        ObjectPooler.SharedInstance.DisableAllObjects();
    }
}
