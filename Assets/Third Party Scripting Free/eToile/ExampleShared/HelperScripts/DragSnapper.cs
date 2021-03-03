using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Snap a scroll rect to its children items. All self contained.
/// Note: Only supports 1 direction
/// </summary>
/// 

[RequireComponent(typeof(ScrollRect))]

public class DragSnapper : UIBehaviour, IEndDragHandler, IBeginDragHandler
{
	public SnapDirection direction; // the direction we are scrolling
	public AnimationCurve decelerationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // a curve for transitioning
	public float speed = 0.5f; // the speed in which we snap ( normalized position per second? )
	public float snapPercentage = 0.2f;	//Porcentaje de desplazamiento para saltar a la siguiente pantalla.
	public int startItem = 0;   //Item para mostrar al inicio.

    ScrollRect scrollRect; // the scroll rect to scroll
    int itemCount; // how many items we have in our scroll rect (2 minimo)
    float value;	//Memoriza el ultimo estado valido estatico.
	int target;		//Memoria de la proxima posicion automatica al soltar.

	new void Start()
	{
		// Conectar con los items (vistas) automaticamente:
		itemCount = transform.Find("Container").childCount;
        // Forzar la vista inicial:
        scrollRect = gameObject.GetComponent<ScrollRect>();
		target = startItem;
		scrollRect.normalizedPosition = new Vector2(startItem / (float) itemCount, 0f);
		value = scrollRect.normalizedPosition.x;
        // Configurar tipo de scroll del ScrollRect:
        if (direction == SnapDirection.Horizontal)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
        }
        else if (direction == SnapDirection.Vertical)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }
    }

    // The direction we are snapping in
    public enum SnapDirection
	{
		Horizontal,
		Vertical,
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		StopCoroutine(SnapRect()); // if we are snapping, stop for the next input
	}
	
	public void OnEndDrag(PointerEventData eventData)
	{
		StartCoroutine(SnapRect()); // simply start our coroutine ( better than using update )
	}

	private IEnumerator SnapRect()
	{
		if(itemCount < 2) print ("Item count must be 2 or more");

		float delta = 1f / (itemCount - 1); //Porcentage de cada item (por si se cambia en dinamico).
		float startNormal = direction == SnapDirection.Horizontal ? scrollRect.horizontalNormalizedPosition : scrollRect.verticalNormalizedPosition;
		float tempDelta = (startNormal - value) / delta;	//Porcentaje de desplazamiento.

		//Pantalla mas cercana en base a la posicion actual.
		target = Mathf.RoundToInt(value/delta);
		if(tempDelta > snapPercentage) target++;
		else if(tempDelta < -snapPercentage) target--;
		target = Mathf.Clamp(target, 0, itemCount - 1);

		float endNormal = delta * target; //Valor normalizado de la pantalla de destino.
		float duration = Mathf.Abs((endNormal - startNormal) / speed); //Tiempo necesario para alcanzar el destino segun la velocida configurada.
		
		float timer = 0f;
		while (timer < 1f) //Animar la desaceleracion de desplazamiento.
		{
			timer = Mathf.Min(1f, timer + Time.deltaTime / duration); // calculate our timer based on our speed
			value = Mathf.Lerp(startNormal, endNormal, decelerationCurve.Evaluate(timer)); // our value based on our animation curve, cause linear is lame
			
			if (direction == SnapDirection.Horizontal) // depending on direction we set our horizontal or vertical position
				scrollRect.horizontalNormalizedPosition = value;
			else
				scrollRect.verticalNormalizedPosition = value;

			yield return new WaitForEndOfFrame(); // wait until next frame
		}
	}

	/*
	protected override void Reset()
	{
		base.Reset();
		if (scrollRect == null) // if we are resetting or attaching our script, try and find a scroll rect for convenience 
			scrollRect = GetComponent<ScrollRect>();
	}*/
}
