using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OriginObject : MonoBehaviour {

	// name of the asset that this object is made from
	public string originObjectName;

	// if the object has multiple pieces it requires different loading since it will be loaded twice otherwise
	public bool originObjectHasMultipleMeshes = false;

	// name of the child mesh from the origin object with multiple meshes
	public string meshName;


}
