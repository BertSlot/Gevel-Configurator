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

<<<<<<< HEAD
	/// <summary>
	/// progress bar
	/// </summary>
	public Slider slider;

	/// <summary>
	/// status text, TextMeshPro Object
	/// </summary>
	public TMP_Text statusText;

	/// <summary>
	/// progress bar fill. Can be found under FillArea
	/// </summary>
	public Image barColorImage;

	/// <summary>
	/// Target progress bar value
	/// </summary>
	private float targetProgress;

	/// <summary>
	/// speed at wich the progress vbar moves towards target progress. 0.5 means it takes 2 seconds to get from 0 to full. So 1/fillspeed = seconds
	/// </summary>
	public float fillSpeed = 0.5f;

	/// <summary>
	/// Default green color for the progress bar
	/// </summary>
=======
	public Slider slider;
	public TMP_Text statusText;
	public Image barColorImage;

	private float targetProgress;
	public float fillSpeed = 0.5f;

>>>>>>> PointCloudLoading
	private Color defaultColor = new Color(0, 255, 0);

	private void OnDisable() {
		targetProgress = 0;
		slider.value = 0;
<<<<<<< HEAD
		statusText.text = "None";
=======
>>>>>>> PointCloudLoading
		barColorImage.color = defaultColor;
	}

	private void Start() {
<<<<<<< HEAD
		// resets all aspects of the loading screen
		targetProgress = 0;
		slider.value = 0;
		barColorImage.color = defaultColor;
		statusText.text = "None";
	}

	void Update() {
		// Moves slider towards target progress based on speed and delta time
		if (slider.value < targetProgress) {
			slider.value += fillSpeed * Time.deltaTime;
		} else if (slider.value == 1) {
			// doesn't need to do anything at 100% this jsut dispalys finished
=======
		targetProgress = 0;
		slider.value = 0;
		barColorImage.color = defaultColor;
		SettargetProgressBarValue(1f);
	}

	void Update() {
		if (slider.value < targetProgress) {
			slider.value += fillSpeed * Time.deltaTime;
		} else if (slider.value == 1) {
>>>>>>> PointCloudLoading
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

<<<<<<< HEAD

	/// <summary>
	/// Use this function to start a coroutine that disables the Loading Screen after 'x' amount of seconds
	/// </summary>
	/// <param name="seconds"></param>
	public void DisableAfterSeconds(float seconds) {
		StartCoroutine(WaitSeconds(seconds));
	}

	/// <summary>
	/// IEnumerator for coroutine that disables the Loading Screen after X amount of seconds
	/// </summary>
	/// <param name="seconds"></param>
	/// <returns></returns>
	private IEnumerator WaitSeconds(float seconds) {
=======
	public void DisableAfterSeconds(float seconds) {
		StartCoroutine(WaitSeconds(seconds));
	}
	IEnumerator WaitSeconds(float seconds) {
>>>>>>> PointCloudLoading
		yield return new WaitForSeconds(seconds);
		gameObject.SetActive(false);
	}

}
//}