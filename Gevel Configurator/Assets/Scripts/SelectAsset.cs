using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectAsset : MonoBehaviour, IPointerDownHandler {

	public GameObject Asset;
	[SerializeField]
	private Text assetNameText; //null reference exception
	public string AssetNameText {
		get { return assetNameText.text; }
		set { assetNameText.text = value; }
	}

	private RTG.GizmoManager gizmoManager;


	// Click data for Asset  Menu clicks
	float clicked = 0;
	float clicktime = 0;
	readonly float clickdelay = 0.5f;



	// Start is called before the first frame update
	void Start() {
		assetNameText = GetComponentInChildren<Text>();
		gizmoManager = GameObject.Find("RTGizmoManager").GetComponent<RTG.GizmoManager>();

	}

	public void OnPointerDown(PointerEventData eventData) {
		clicked++;
		if (clicked == 1)
			clicktime = Time.time;

		if (clicked > 1 && Time.time - clicktime < clickdelay) {
			clicked = 0;
			clicktime = 0;
			GameObject.Find("AssetsMenu").SetActive(false);
			gizmoManager.SpawnObjectMiddle(Asset);



		} else if (clicked > 2 || Time.time - clicktime > 1)
			clicked = 0;

	}

}
