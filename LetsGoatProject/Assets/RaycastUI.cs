//Attach this script to your Canvas GameObject.
//Also attach a GraphicsRaycaster component to your canvas by clicking the Add Component button in the Inspector window.
//Also make sure you have an EventSystem in your hierarchy.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class RaycastUI : MonoBehaviour
{
    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

    private bool _globalFocus;

    void Start()
    {
        //Fetch the Raycaster from the GameObject (the Canvas)
        m_Raycaster = GetComponent<GraphicRaycaster>();
        //Fetch the Event System from the Scene
        m_EventSystem = EventSystem.current;
    }

    private void OnApplicationFocus(bool focus)
    {
        _globalFocus = focus;

        Debug.Log(_globalFocus);
    }


    void Update()
    {
        if (m_Raycaster == null || m_EventSystem == null) return;
        if (_globalFocus)
        {
            //Check if the left Mouse button is clicked
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                //Set up the new Pointer Event
                m_PointerEventData = new PointerEventData(m_EventSystem);
                //Set the Pointer Event Position to that of the mouse position
                m_PointerEventData.position = Input.mousePosition;

                //Create a list of Raycast Results
                List<RaycastResult> results = new List<RaycastResult>();

                //Raycast using the Graphics Raycaster and mouse click position
                m_Raycaster.Raycast(m_PointerEventData, results);

                //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject.layer == LayerMask.NameToLayer("InteractibleUI"))
                    {
                        if (result.gameObject.GetComponent<Button>() != null)
                        {
                            result.gameObject.GetComponent<Button>().onClick.Invoke();
                            _globalFocus = false;
                            Debug.Log("Hit " + result.gameObject.name);
                        }
                    }
                }
            }
        }
    }
}