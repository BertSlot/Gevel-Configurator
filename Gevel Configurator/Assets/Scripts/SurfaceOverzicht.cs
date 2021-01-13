using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SurfaceOverzicht : MonoBehaviour {

	[SerializeField]
	private GameObject Overzicht;

	[SerializeField]
	private GameObject overzichtContent;

	private Transform SurfaceNameList;
	private Transform SurfaceTotalsList;

	private Objects_overzicht overzicht;


	private void Awake() {

		overzicht = Overzicht.GetComponent<Objects_overzicht>();

		SurfaceTotalsList = GameObject.Find("Group Totals").transform;
		SurfaceNameList = GameObject.Find("Group Names").transform;


	}

	private void OnEnable() {
		//ClearContents();
		RenderOverzichtTotals();

	}



	private void OnDisable() {
		ClearContents();

	}

	public void RenderOverzichtTotals() {
		ClearContents();
		foreach (var group in overzicht.SurfaceGroupTotals.Keys) {
			var groupTextbox = CreateListObject(group);
			groupTextbox.transform.SetParent(SurfaceNameList, false);
		}

		foreach (var total in overzicht.SurfaceGroupTotals.Values) {
			// values to string with cm^2 or M^2
			// stuff ?????????????????? stuff

			var groupTextbox = CreateListObject(total.ToString() + "cm^2");
			groupTextbox.transform.SetParent(SurfaceTotalsList, false);
		}

		RectTransform groupViewRt = overzichtContent.GetComponent<RectTransform>();
		groupViewRt.sizeDelta = new Vector2(0, 20 * SurfaceNameList.childCount);
	}

	void ClearContents() {
		foreach (Transform group in SurfaceNameList) {
			//group.SetParent(null, false);
			Destroy(group.gameObject);
		}
		foreach (Transform total in SurfaceTotalsList) {
			//total.SetParent(null, false);
			Destroy(total.gameObject);
		}

		RectTransform groupViewRt = overzichtContent.GetComponent<RectTransform>();
		groupViewRt.sizeDelta = new Vector2(0, 20 * SurfaceNameList.childCount);
	}


	GameObject CreateListObject(string name) {
		GameObject childObject = new GameObject(name, typeof(RectTransform));

		RectTransform rt = childObject.GetComponent<RectTransform>();
		rt.sizeDelta = new Vector2(60, 20);

		// Text styling
		Text childText = childObject.AddComponent<Text>();
		childText.text = name;
		childText.color = Color.black;
		childText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;

		return childObject;
	}


}
