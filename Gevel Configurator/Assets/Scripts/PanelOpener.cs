using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PanelOpener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
	public GameObject Panel;

	// Start is called before the first frame update
	void Start() {
		Panel.gameObject.SetActive(false);
	}

	// Needed so that when you exit the panel away from the parent panel it stil disables it before the script gets disabled by a higher level panel opener
	void OnDisable() {
		Panel.gameObject.SetActive(false);
	}

	public void OnPointerEnter(PointerEventData pointerEventData) {
		Panel.gameObject.SetActive(true);
	}

	public void OnPointerExit(PointerEventData pointerEventData) {
		Panel.gameObject.SetActive(false);
	}
}
