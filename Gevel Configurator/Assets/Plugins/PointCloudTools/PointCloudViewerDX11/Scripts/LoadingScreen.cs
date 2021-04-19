// Point Cloud Binary Viewer DX11 for runtime parsing
// http://unitycoder.com

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;


// class created for RuntimeViewerDX11.cs
// namespace PointCloudRuntimeViewer {
//internal 
class LoadingScreen : MonoBehaviour {

	public Slider slider;
	public TMP_Text statusText;
	public Image barColorImage;

	private float targetProgress;
	public float fillSpeed = 0.5f;

	private Color defaultColor = new Color(0, 255, 0);

	private void OnDisable() {
		targetProgress = 0;
		slider.value = 0;
		barColorImage.color = defaultColor;
	}

	private void Start() {
		targetProgress = 0;
		slider.value = 0;
		barColorImage.color = defaultColor;
		SettargetProgressBarValue(1f);
	}

	void Update() {
		if (slider.value < targetProgress) {
			slider.value += fillSpeed * Time.deltaTime;
		} else if (slider.value == 1) {
			SetStatusText("Finished");
		}
	}

	/// <summary>
	/// This function sets the targetprogress to the given value which can range from 0 to 1
	/// </summary>
	/// <param name="Value"></param>
	public void SettargetProgressBarValue(float Value) {
		targetProgress = Value;
	}

	/// <summary>
	/// Use this function to set a new status text on the loading screen
	/// </summary>
	/// <param name="status"></param>
	public void SetStatusText(string status) {
		statusText.text = status;
	}

	/// <summary>
	/// Sets progressbar fill color
	/// </summary>
	/// <param name="newColor"></param>
	public void SetBarFillColor(Color newColor) {
		barColorImage.color = newColor;
	}

	public void DisableAfterSeconds(float seconds) {
		StartCoroutine(WaitSeconds(seconds));
	}
	IEnumerator WaitSeconds(float seconds) {
		yield return new WaitForSeconds(seconds);
		gameObject.SetActive(false);
	}

}
//}