using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Save_Dialogue_Script : MonoBehaviour {

	private Text inField;

	// establishes a reference to the input field
	void Awake ()
	{
		inField = transform.Find("InputField").Find("Text").GetComponent<Text>();

		gameObject.SetActive(false);
	}

	// pauses what the EditGM is doing to invoke the save dialogue
	public void invokeDialogue ()
	{
		EditGM.instance.isUpdating = false;
		gameObject.SetActive(true);
	}

	// simply cancels the save dialogue by deactivating the whole panel
	public void cancelDialogue ()
	{
		gameObject.SetActive(false);
		EditGM.instance.isUpdating = true;
	}

	// confirms the file save by passing the entered filename to the EditGM
	public void confirmSave ()
	{
		EditGM.instance.saveFile(inField.text);
		cancelDialogue();
	}
}
