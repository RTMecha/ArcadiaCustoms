using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace ArcadiaCustoms.Functions
{
    public class HoverUI : MonoBehaviour
    {
        private void OnMouseEnter()
        {
            if (ArcadePlugin.PlaySoundOnHover.Value)
            {
                AudioManager.inst.PlaySound("Click");
            }
        }
    }
}
