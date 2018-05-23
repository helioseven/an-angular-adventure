using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveDialogControl : MonoBehaviour {

	private Text inField;

	// establishes a reference to the input field
	void Awake ()
	{
		inField = transform.Find("InputField").Find("Text").GetComponent<Text>();

		gameObject.SetActive(false);
	}

	// pauses what the EditGM is doing to invoke the save dialog
	public void invokeDialog ()
	{
		EditGM.instance.pauseToggle();
		gameObject.SetActive(true);
	}

	// simply cancels the save dialog by deactivating the whole panel
	public void cancelDialog ()
	{
		gameObject.SetActive(false);
		EditGM.instance.pauseToggle();
	}

	// confirms the file save by passing the entered filename to the EditGM
	public void confirmSave ()
	{
		EditGM.instance.saveFile(inField.text);
		cancelDialog();
	}
}
