using RTG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SFB;

public class Objects_overzicht : MonoBehaviour {
	// Start is called before the first frame update

	/// <summary>
	/// Parent object where all the placable objects are childeren from
	/// </summary>
	[SerializeField]
	private GameObject parentObject;


	/// <summary>
	/// Dictionary with group names and group totals
	/// </summary>
	private Dictionary<string, float> surfaceGroupTotals = new Dictionary<string, float>();

	/// <summary>
	/// This function makes sure that whenever you access this variable it is updated
	/// </summary>
	public Dictionary<string, float> SurfaceGroupTotals {
		get {
			UpdateSurfaceGroupTotal();
			return surfaceGroupTotals;
		}
	}
	/*private void Start() {

		foreach (GameObject obj in parentObject.GetAllChildren()) {
			//Debug.Log(obj.name);
			SetSurfaceGroupState(obj, true);
			//AddSurfaceGroup(obj, "Noord");
			//AddSurfaceGroup(obj, "West");
			//AddSurfaceGroup(obj, "Oost");
			//AddSurfaceGroup(obj, "Zuid");
		}
		//ExportTotalsToExcel();
	}
*/

	/*
		private void Update() {

			//Debug.Log(objects[0]);
			//AddSurfaceGroupToList(parentObject.GetAllChildren(), "North");
			//surfaceGroupTotals = TotalSurfaceAreaPerGroup(parentObject.GetAllChildren());
		}
	*/


	/// <summary>
	/// This function exports the SurfaceGroupTotals data to a Excel xls sheet format
	/// </summary>
	public void ExportTotalsToExcel() {

		var path = StandaloneFileBrowser.SaveFilePanel("Save Surface totals as Excel", "", "", "xlsx");
		// Debug.Log(path);
		if (path != null) {
			Excel xls = new Excel();
			ExcelTable table = new ExcelTable {
				TableName = "Totals"
			};
			xls.Tables.Add(table);

			var surfaceTotals = SurfaceGroupTotals;

			xls.Tables[0].SetValue(1, 1, "Surface totals:");
			xls.Tables[0].SetValue(1, 2, "cm^2");
			int iter = 2;

			foreach (var total in surfaceTotals) {
				xls.Tables[0].SetValue(iter, 1, total.Key);
				xls.Tables[0].SetValue(iter, 2, total.Value.ToString());
				iter++;
			}

			ExcelHelper.SaveExcel(xls, path);
			return;
		}

	}


	/// <summary>
	/// This funtion updates the totals of every surface group. Will need to be called before accessing the value.
	/// </summary>
	public void UpdateSurfaceGroupTotal() {
		surfaceGroupTotals = TotalSurfaceAreaPerGroup(parentObject.GetAllChildren());
	}


	/// <summary>
	/// This funciton turns on the surface calculations for this Gameobject and if it didnt have the component it adds it a well
	/// </summary>
	/// <param name="obj"></param>
	public void SetSurfaceGroupState(GameObject obj, bool state) {
		if (obj.GetComponent<SurfaceAreaGroup>() == null) {
			SurfaceAreaGroup groups = obj.AddComponent<SurfaceAreaGroup>();
			groups.surfaceOn = state;
		} else {
			SurfaceAreaGroup groups = obj.GetComponent<SurfaceAreaGroup>();
			groups.surfaceOn = state;
		}
	}




	/// <summary>
	/// This function removes a surface group from the given gameObject
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="groupName"></param>
	public void RemoveSurfaceGroup(GameObject obj, string groupName) {
		if (obj.GetComponent<SurfaceAreaGroup>() != null) {  // chek length of list maybe
			SurfaceAreaGroup groups = obj.GetComponent<SurfaceAreaGroup>();
			if (groups.surfaceGroups.Contains(groupName)) {
				groups.surfaceGroups.Remove(groupName);
			}
		}
	}

	/// <summary>
	/// This funtion removes a SurfaceGroup from a list of GameObjects
	/// </summary>
	/// <param name="objList"></param>
	/// <param name="groupName"></param>
	public void RemoveSurfaceGroupFromList(List<GameObject> objList, string groupName) {

		foreach (GameObject obj in objList) {
			RemoveSurfaceGroup(obj, groupName);
		}
	}




	/// <summary>
	/// This function add a surface group to a given object and if the object doesnt have a surfaceAreaGroup component it adds it as well
	/// </summary>
	/// <param name="obj"></param>
	/// <param name="groupName"></param>
	public void AddSurfaceGroup(GameObject obj, string groupName) {
		if (obj.GetComponent<SurfaceAreaGroup>() == null) {  // chek length of list maybe
			SurfaceAreaGroup groups = obj.AddComponent<SurfaceAreaGroup>();
			groups.surfaceOn = true;
			groups.surfaceGroups.Add(groupName);
		} else {
			SurfaceAreaGroup groups = obj.GetComponent<SurfaceAreaGroup>();
			groups.surfaceOn = true;
			if (!groups.surfaceGroups.Contains(groupName)) {
				groups.surfaceGroups.Add(groupName);
			}
		}
	}

	/// <summary>
	/// This function adds a surfaceGroup to a list of GameObjects
	/// </summary>
	/// <param name="objList"></param>
	/// <param name="groupName"></param>
	public void AddSurfaceGroupToList(List<GameObject> objList, string groupName) {
		foreach (GameObject obj in objList) {
			AddSurfaceGroup(obj, groupName);
		}
	}



	/// <summary>
	/// This function will calculate the total surface area for each surfaceGroup and also return the amount obects without a facing
	/// </summary>
	/// <param name="objList"></param>
	/// <returns></returns>
	public static Dictionary<string, float> TotalSurfaceAreaPerGroup(List<GameObject> objList) { // this function will use the calculatesurfaceArea function

		Dictionary<string, float> groupTotals = new Dictionary<string, float>();
		List<string> groups;
		foreach (GameObject obj in objList) {

			if (obj.GetComponent<SurfaceAreaGroup>() == null) {
				obj.AddComponent<SurfaceAreaGroup>();
			}
			SurfaceAreaGroup objSurfaceGroup = obj.GetComponent<SurfaceAreaGroup>();
			groups = objSurfaceGroup.surfaceGroups;
			if (groups.Count != 0) {  // NullReferenceException
				float surfaceArea = CalculateSurfaceArea(obj);

				foreach (string group in groups) {
					if (groupTotals.ContainsKey(group)) {
						groupTotals[group] += surfaceArea;
					} else {
						groupTotals.Add(group, surfaceArea);
					}
				}
			} else {
				if (groupTotals.ContainsKey("No Surface Group")) {
					groupTotals["No Surface Group"] += 1;
				} else {
					groupTotals.Add("No Surface Group", 1);
				}
			}

		}

		return groupTotals;
	}



	/// <summary>
	/// Calculates the surface area of an object using the object.transform.localScale .x and .y 
	/// </summary>
	public static float CalculateSurfaceArea(GameObject obj) {
		Vector3 meshSize = obj.transform.localScale;
		return (float)(meshSize.x * meshSize.y); // made into float to satisfy the output value criteria
	}

	/// <summary>
	/// This fucnion returns the total surface are of all objects in a given list.
	/// </summary>
	/// <param name="objList"></param>
	/// <returns></returns>
	public static float CalculateTotalSurfaceArea(List<GameObject> objList) {
		float total = 0;
		foreach (GameObject obj in objList) {
			total += CalculateSurfaceArea(obj);
		}
		return (float)total;
	}

}
