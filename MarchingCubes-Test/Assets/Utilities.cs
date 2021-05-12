using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Utilities
{
    public static class Utilities
    {

        /**
        * Rounds a vectors components to their nearest ints
        **/
        public static Vector3 Round(this Vector3 vect)
        {
            vect.x = Mathf.Round(vect.x);
            vect.y = Mathf.Round(vect.y);
            vect.z = Mathf.Round(vect.z);
            return vect;
        }
    }

}
