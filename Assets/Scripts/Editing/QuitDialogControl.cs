using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitDialogControl : MonoBehaviour {

	// establishes a reference to the input field
	void Awake ()
	{
		gameObject.SetActive(false);
	}

	// pauses what the EditGM is doing to invoke the quit dialog
	public void invokeDialog ()
	{
		EditGM.instance.gameObject.SetActive(false);
		gameObject.SetActive(true);
	}

	// simply cancels the quit dialog by deactivating the whole panel
	public void cancelDialog ()
	{
		gameObject.SetActive(false);
		EditGM.instance.gameObject.SetActive(true);
	}

	// confirms the file save by passing the entered filename to the EditGM
	public void confirmQuit ()
	{
		cancelDialog();
		EditGM.instance.returnToMainMenu();
	}
}