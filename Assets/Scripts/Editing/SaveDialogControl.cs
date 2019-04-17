using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveDialogControl : MonoBehaviour {

	private Text path_field;

	void Awake ()
	{
		path_field = transform.Find("InputField").Find("Text").GetComponent<Text>(); // <1>

		gameObject.SetActive(false);

		/*
		<1> establishes a reference to the relevant Text component
		*/
	}

	// pauses what the EditGM is doing to invoke the save dialog
	public void invokeDialog ()
	{
		EditGM.instance.gameObject.SetActive(false);
		gameObject.SetActive(true);
	}

	// cancels the save dialog by deactivating the panel and resuming EditGM
	public void cancelDialog ()
	{
		gameObject.SetActive(false);
		EditGM.instance.gameObject.SetActive(true);
	}

	// confirms the file save by passing the entered filename to the EditGM
	public void confirmSave ()
	{
		EditGM.instance.SaveFile(path_field.text);
		cancelDialog();
	}
}
