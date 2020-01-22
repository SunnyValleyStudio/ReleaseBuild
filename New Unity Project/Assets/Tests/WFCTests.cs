using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Tests
{
    public class WFCTests
    {
        Tilemap input;
        Tilemap output;
        WFCHumbleObject wfc;

        [SetUp]
        public void Init()
        {
            var loadedInputMapResource = Resources.Load("InputMap") as GameObject;
            input = loadedInputMapResource.GetComponent<Tilemap>();
            var loadedOutputMapResource = Resources.Load("OutputMap") as GameObject;
            output = loadedOutputMapResource.GetComponent<Tilemap>();
            wfc = new WFCHumbleObject(input, output, 4, true);
        }
        // A Test behaves as an ordinary method
        [Repeat(20)]
        [Test]
        public void WFCTestsSimplePasses()
        {
            bool result = wfc.RunWFC();
            Assert.IsTrue(result);
        }

    }
}
