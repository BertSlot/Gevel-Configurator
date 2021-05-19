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

	/// <summary>
	/// progress bar
	/// </summary>
	public Slider slider;

	/// <summary>
	/// status text, TextMeshPro Object
	/// </summary>
	public TMP_Text statusText;

	private bool disablePending = false;
	private float pendingSeconds;

	private bool newStatusPending = false;
	private string newStatusText;

	/// <summary>
	/// progress bar fill. Can be found under FillArea
	/// </summary>
	public Image barColorImage;

	public GameObject loadingPanel;

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
	private Color defaultColor = new Color(0, 255, 0);

	private void OnDisable() {
		targetProgress = 0;
		slider.value = 0;
		statusText.text = "None";
		barColorImage.color = defaultColor;
	}

	private void Start() {
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
			//SetStatusText("Finished");
		}

		if (newStatusPending)
			SetStatusText(newStatusText);
		if (disablePending)
			DisableAfterSeconds(pendingSeconds);

	}

	/// <summary>
	/// This function sets the targetprogress to the given value which can range from 0 to 1
	/// </summary>
	/// <param name="Value"></param>
	public void SetTargetProgressBarValue(float Value) {
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
	/// TextMeshPro UI objects cant be modified from a thread, so to circumvent that this function 
	/// sets a bool to signal the loading Screen to update the status text in the next Update() call
	/// </summary>
	/// <param name="statusString"></param>
	public void SetStatusTextThread(string statusString) {
		newStatusText = statusString;
		newStatusPending = true;
	}

	/// <summary>
	/// Sets progressbar fill color
	/// </summary>
	/// <param name="newColor"></param>
	public void SetBarFillColor(Color newColor) {
		barColorImage.color = newColor;
	}


	/// <summary>
	/// Use this function to start a coroutine that disables the Loading Screen after 'x' amount of seconds
	/// </summary>
	/// <param name="seconds"></param>
	public void DisableAfterSeconds(float seconds) {
		StartCoroutine(WaitSeconds(seconds));
	}

	/// <summary>
	/// UI objects cant be modified from a thread, so to circumvent that this function 
	/// sets a bool to signal the loading Screen to disable itself
	/// </summary>
	/// <param name="seconds"></param>
	public void DisableAfterSecondsThread(float seconds) {
		disablePending = true;
		pendingSeconds = seconds;
	}

	/// <summary>
	/// Enables the loading screen.
	/// </summary>
	public void EnableLoadingScreen() {
		loadingPanel.gameObject.SetActive(true);
	}

	/// <summary>
	/// IEnumerator for coroutine that disables the Loading Screen after X amount of seconds
	/// </summary>
	/// <param name="seconds"></param>
	/// <returns></returns>
	private IEnumerator WaitSeconds(float seconds) {
		yield return new WaitForSeconds(seconds);
		loadingPanel.SetActive(false);
	}

}
//}