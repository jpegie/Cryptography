using FiatShamiraProtocol.Модель.Дополнения;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FiatShamiraProtocol.Модель;
public class VerificationCenter
{
    const int VERIFYING_ROUNDS_AMOUNT = 20;
    BigInteger _modulo;
    Random _rnd;
    public VerificationCenter(BigInteger modulo)
    {
        _rnd = new Random();
        _modulo = modulo;
    }
    /*
     * Каждая аккредитация состоит из следующих этапов:
     * 1. А выбирает случайное r из интервала (1, n−1) и отсылает x = r^2 (mod n) стороне B.
     * 2. B случайно выбирает бит e (0 или 1) и отсылает его A.
     * 3. А вычисляет y = r*s^e (mod n) и отправляет его обратно к B.
     * 4. Сторона B проверяет равенство y^2 ≡ x*v^e (mod n). Если оно верно, 
     * то происходит переход к следующему раунду протокола, 
     * иначе доказательство не принимается.
     */
    public bool VerifyUser(User requester, User verifiable)
    {
        var isUserVerified = false;
       
        for (int i = 0; i < VERIFYING_ROUNDS_AMOUNT; ++i)
        {
            BigInteger verifiableUserX = (BigInteger) MessagingCenter.RequestValue(verifiable, "x");
            MessagingCenter.SendMessage(verifiable, requester, verifiableUserX);
            int requesterE = (int)MessagingCenter.RequestValue(requester, "e");
            MessagingCenter.SendMessage(requester, verifiable, requesterE);
            BigInteger verifiableUserY = (BigInteger)MessagingCenter.RequestValue(verifiable, "y");
            MessagingCenter.SendMessage(verifiable, requester, verifiableUserY);
            isUserVerified = (bool)MessagingCenter.RequestValue(requester, "equality");
            if (!isUserVerified)
            {
                break;
            }
        }
        return isUserVerified;
    }
    public BigInteger GenerateR()
    {
        return _rnd.NextBigInteger(1, _modulo);
    }
    public int GenerateE()
    {
        return _rnd.Next(0, 1);
    }
    public BigInteger CalculateY(BigInteger r, int e, BigInteger privateKey, BigInteger modulo)
    {
        return (r * BigInteger.ModPow(privateKey, e, modulo)) % modulo;
    }
    public bool VerifyEquality(BigInteger y, BigInteger x, BigInteger publicKey, int e, BigInteger modulo)
    {
        var left = BigInteger.ModPow(y, 2, modulo);
        var right = (x % modulo) * BigInteger.ModPow(publicKey, e, modulo);
        var equals = left == right;
        return equals;
    }
}
