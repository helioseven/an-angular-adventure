using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuGM : MonoBehaviour {

	// Play button ref
	public Button playButton;
	// Edit button ref
	public Button editButton;
	// PlayLoader ref
	public GameObject playLoader;
	// EditLoader ref
	public GameObject editLoader;

	void Awake ()
	{		
		playButton.onClick.AddListener(startPlay);
		editButton.onClick.AddListener(startEdit);
	}

	private void startPlay ()
	{
		Instantiate(playLoader);
	}

	private void startEdit ()
	{
		Instantiate(editLoader);
	}
}