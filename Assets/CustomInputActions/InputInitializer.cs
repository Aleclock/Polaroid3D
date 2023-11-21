using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputInitializer : MonoBehaviour
{
    [SerializeField] InputReader inputReader;

    private void OnEnable() {
        if (inputReader != null)
            inputReader.EnableInputs();
    }
}
