using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Assets.Nimbatus.Scripts.WorldObjects.Items.DroneParts;
using Assets.Nimbatus.Scripts.WorldObjects.Items.DroneParts.SensorParts;
using Assets.Nimbatus.Scripts.WorldObjects.Items.Selection;
using BepInEx;
using BepInEx.Preloader;
using HarmonyLib;
using NCalc;
using Sirenix.Utilities;
using UnityEngine;

namespace Nimbatus_tag_variables
{

    [BepInProcess("Nimbatus.exe")]
    [BepInPlugin("uniquename.nimbatus.tag-vars", "tag-vars", "0.0.0.0")]
    public class Nimbatus_tag_variables : BaseUnityPlugin
    {
        public void Awake()
        {
            var harmony = new Harmony("uniquename.nimbatus.tag-vars");
            harmony.PatchAll();
        }
    }
    
    
    [HarmonyPatch(typeof(EventKeyHub), "PressKey", argumentTypes: new Type[] {typeof(bool), typeof(string)})]
    public class PressKey_Patch
    {
        private static Dictionary<string, int> vars = new Dictionary<string, int>{};
        private static Dictionary<string, string> active = new Dictionary<string, string>{};

        public static void Prefix(ref bool press, ref string keyCode, EventKeyHub __instance)
        {
            //keyCode is the tag
            string orig_keyCode = keyCode;
            
            //regex to find things within {}
            Regex rx = new Regex(@"\{([^\}]+)\}"); 
            MatchCollection equations = rx.Matches(keyCode);
            
            //iterates through each substring within {} found
            for (int count = 0; count < equations.Count; count++)
            {
                string[] tmp2 = equations[count].Value.Replace("{", "").Replace("}", "").Split('=');

                //replaces var names with their value within tag
                foreach (KeyValuePair<string, int> var in vars)
                {
                    tmp2[tmp2.Length - 1] = tmp2[tmp2.Length - 1].Replace(var.Key, var.Value.ToString());
                }
                
                // gets value of equation
                Expression e = new Expression(tmp2[tmp2.Length - 1]);
                var ans = e.Evaluate();

                
                if (tmp2.Length > 1 && !press) //!press = release of key
                {
                    vars[tmp2[0].Replace(" ", "")] = int.Parse(ans.ToString()); //right of = is assigned to left var
                    // removes spaces in case it is written like {a = 5 + 5} or { a = 5 + 5 }, so that it will just be "a" and not "a " or " a "
                }

                // replaces equation with it's value
                keyCode = keyCode.Replace(equations[count].ToString(), int.Parse(ans.ToString()).ToString());
            }
            
            
            //added to fix problem with releasing keys
            if (press)
            {
                active.Add(orig_keyCode, keyCode);
            }
            else if (active.ContainsKey(orig_keyCode))
            {
                keyCode = active[orig_keyCode];
                active.Remove(orig_keyCode);
            }
            
        }
    }
}