using System.Linq;
using UnityEngine;

public class Car : Vehicle
{
    // Returns the center of the front of the box collider
    public override Vector3 GetBumperOffset()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        return new Vector3(boxCollider.center.x, boxCollider.center.y, boxCollider.center.z + boxCollider.size.z / 2);
    }

    void OnDrawGizmos()
    {
        // car status check
        switch (status)
        {
            case Status.DRIVING:
                Gizmos.color = Color.green;
                break;
            case Status.WAITING_CAR:
                Gizmos.color = Color.black;
                break;
            case Status.WAITING_RED:
                Gizmos.color = Color.red;
                break;
        }

        Gizmos.DrawCube(bumperPosition + Vector3.up, Vector3.one);
    }

    //void PlayEffect()
    //{
    //    if (carEventEffect == null)
    //        return;

    //    var effect = Instantiate(carEventEffect, transform.position + carCollider.center, Quaternion.identity);
    //    var settings = effect.main;

    //    settings.loop = false;
    //    settings.useUnscaledTime = true;
    //    effect.Play();

    //    Destroy(effect.gameObject, Time.timeScale * settings.duration);
    //}
}