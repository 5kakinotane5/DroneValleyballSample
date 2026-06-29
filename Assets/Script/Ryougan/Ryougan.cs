// 2台のカメラの視線から3D座標を求める三角測量モジュール。

using UnityEngine;

public static class Ryougan
{
    public static bool CanGetPosition(Ray a, Ray b)
    {
        return !IsPrallel(a, b);
    }

    // 2本の視線（Ray）から3D座標を求める。
    public static Vector3 GetPosition(Ray a, Ray b)
    {
        if (!CanGetPosition(a, b))
        {
            throw new System.Exception("視線の幾何学的条件により、3D座標を求められません。");
        }

        Vector3 da = a.direction;
        Vector3 db = b.direction;
        Vector3 r = a.origin - b.origin;

        float dad = Vector3.Dot(da, da); // ≒1
        float dbd = Vector3.Dot(db, db); // ≒1
        float dadb = Vector3.Dot(da, db);
        float denom = dad * dbd - dadb * dadb; // = 1 - (da・db)^2

        float dar = Vector3.Dot(da, r);
        float dbr = Vector3.Dot(db, r);

        float t = (dadb * dbr - dbd * dar) / denom; // 直線A上のパラメータ
        float s = (dad * dbr - dadb * dar) / denom; // 直線B上のパラメータ

        Vector3 closestA = a.origin + da * t;
        Vector3 closestB = b.origin + db * s;
        return (closestA + closestB) * 0.5f; // 2つの最近接点の中点
    }

    private static bool IsPrallel(Ray a, Ray b)
    {
        Vector3 da = a.direction;
        Vector3 db = b.direction;
        float dadb = Vector3.Dot(da, db);
        float denom = 1 - dadb * dadb; // = 1 - (da・db)^2
        return Mathf.Abs(denom) <= 1e-6f;
    }
}
