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

	// Update is called once per frame
	//void Update(){

	//}

	public void OnPointerEnter(PointerEventData pointerEventData) {
		Panel.gameObject.SetActive(true);
	}

	public void OnPointerExit(PointerEventData pointerEventData) {
		Panel.gameObject.SetActive(false);
	}
}
