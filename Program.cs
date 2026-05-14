using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Console;

namespace AnimazioneBufferConsole
{
    internal class Program
    {

        /* Semplice esempio di battaglia spaziale (senza scorrimento dello sfondo)
        * Aggiunte le modifiche da mettere in atto.
        *
        * 1. XXXXXXXXXXXXXXX Permettere all'utente di sparare un raggio "|", deve partire dalla giusta
        * posizione rispetto alla sua navicella, parte alla pressione del tasto Shift
        * (funziona mentre è premuto un altro tasto) o della barra spazio (come
        * altri tasti il programma ne legge uno alla volta, l’ultimo premuto),
        * un solo raggio attivo alla volta.
        *
        * 2. XXXXXXXXXXXXXXX Permettere all'astronave controllata dal computer di sparare, in automatico,
        * un flare "*", uno alla volta. Ma in quale condizione deve sparare?
        *
        * 3. XXXXXXXXXXXXXXX Verificare l'eventuale collisione tra una nave e il colpo nemico e mostrare
        * per qualche secondo l'ASCII ART di un'esplosione.
        *
        * 4. Suddividere opportunamente tutto il codice con metodi e relativi parametri e
        * non usare i campi di classe. Usare i commenti <summary> completi per ogni
        * metodo.
        *
        * 5. XXXXXXXXXXXXXXX L'utente deve avere 3 vite, visualizzare in alto a sinistra e destra il numero
        * di vite rimanenti e il numero di nemici abbattuti.
        *
        * 6. Al posto dei valori letterali utilizzate le costanti (non per l'ASCII ART).
        *
        * 7. Per ridimensionare la finestra usate i metodi
        * Console.SetBufferSize(numeroColonne, numeroRighe);
        * Console.SetWindowSize(numeroColonne, numeroRighe);

        * 8. EXTRA:
        *
        * 1.  XXXXXXXXXXXXXXXXX Aggiungere il movimento verticale a utente e astronave fino a
        * metà schermo o poco più, allo stato attuale non è consentita la
        * gestione contemporanea di più tasti, tranne per i modificatori
        * come Shift che può essere letto solo in contemporanea ad altri tasti.
        *
        *
        * 2.  XXXXXXXXXXXXXXXXX Scrolling continuo e verticale dello sfondo.
        *
        * 3.  XXXXXXXXXXXXXXXXX Più nemici in contemporanea e con percorsi diversi.
        *
        * 4. Animare l'esplosione.
        *
        * 5.  XXXXXXXXXXXXXXXXX Usare i colori per ogni singolo carattere (vedi poi). Altro???
        */
        static void Main(string[] args)
        {
            Console.Title = "Battaglia spaziale";

            // ASCII Art.
            (int scostamento, string riga)[] astronave = { // Astronave.
                (2,   @"___"),
                (1,  @"/   \"),
                (1,  @"|o o|"),
                (1,  @"\___/"),
                (3,    @"|")
                };

            (int scostamento, string riga)[] naveUtente = { // Nave utente.
                (1,  @"_|_ "),
                (0, @"/_ _\"),
                (0, @"\   /"),
                (1,  @"'v' ")
                };

            (int scostamento, string riga)[] esplosione = { // Esplosione.
                (-1,  @". . . ."),
                (-3,@". . . . . ."),
                (-3,@". . . . . ."),
                (-3,@". . . . . ."),
                (0,    @". . .")
                };

            // Colpo da parte dell'utente.
            (int scostamento, char riga)[] colpoUtente = new (int, char)[] {
                                                                               (3, '|'), (3, '|'), (3, '|'), (3, '|'), (3, '|'),
                                                                               (3, '|'), (3, '|'), (3, '|'), (3, '|'), (3, '|')
                                                                           };


            const int LUNGHEZZA_BUFFER = 30;
            const int LARGHEZZA_BUFFER = 80;
            const int NUMERO_MASSIMO_PROIETTILI_UTENTE = 30;
            const int TEMPO_FRAME = 20;
            const int X_MESSAGGIO_FINALE = 25;
            const int Y_MESSAGGIO_FINALE = 15;

            bool incAstronave1 = true;

            bool incAstronave2 = true;

            bool incAstronave3 = true;

            // Buffer dello schermo da scrivere nella console ad ogni fotogramma.
            (ConsoleColor colore, char carattere)[,] bufferSchermo = new (ConsoleColor colore, char carattere)[LUNGHEZZA_BUFFER, LARGHEZZA_BUFFER];


            char[,] sfondoSchermo;// Sfondo dello schermo.

            ConsoleKeyInfo tastoPremuto = new ConsoleKeyInfo(); // Per la lettura dei tasti.

            int xU = (LARGHEZZA_BUFFER - 5) / 2; // X nave utente.

            int yU = LUNGHEZZA_BUFFER - 5; // Y nave utente.

            int x1 = 39, y1 = 1; // Coordinate x1 e y1 dell'astronave nemica1

            int x2 = 59, y2 = 10; // Coordinate x1 e y1 dell'astronave nemica3  

            int x3 = 70, y3 = 20; // Coordinate x1 e y1 dell'astronave nemica3


            int randomProiettileNemico = 18;
            int nemiciDaAbbatterePerPassaggioLivello = 5;

            int livello = 1;


            // Inizializza il buffer e lo sfondo dello schermo.          


            sfondoSchermo = new char[LUNGHEZZA_BUFFER, LARGHEZZA_BUFFER];

            Random rnd = new Random(); // Generatore di numeri casuali.


            InizializzazioneSfondo(rnd, sfondoSchermo, bufferSchermo);

            //for (int i = 0; i < bufferSchermo.GetLength(0); i++)
            //{
            //    for (int j = 0; j < bufferSchermo.GetLength(1); j++)
            //    {
            //        sfondoSchermo[i, j] = rnd.Next(0, 17) < 1 ? '.' : ' ';
            //    }
            //}


            // Imposta il colore in primo piano (caratteri) e dello sfondo.
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.BackgroundColor = ConsoleColor.Black;


            // Rende il cursore di output invisibile e imposta
            // il formato dei caratteri per rendere visibile '☼'.
            // Il cursore può comparire di nuovo se la finestra viene
            // ridimensionata: risolvibile inserendo il comando
            // nel ciclo dell'animazione.

            Console.CursorVisible = false;
            Console.OutputEncoding = System.Text.Encoding.UTF8;


            bool[] attivo = new bool[NUMERO_MASSIMO_PROIETTILI_UTENTE];
            int[] xProiettili = new int[30];
            int[] yProiettili = new int[30];

            int xProiettileNemico1 = 0;
            int yProiettileNemico1 = 0;

            int xProiettileNemico2 = 0;
            int yProiettileNemico2 = 0;

            int xProiettileNemico3 = 0;
            int yProiettileNemico3 = 0;

            bool attivazioneSparatoriaAstronaveNemica1 = false;
            bool attivazioneSparatoriaAstronaveNemica2 = false;
            bool attivazioneSparatoriaAstronaveNemica3 = false;

            int numeroViteRimanenti = 3;
            int nemiciAbbattuti = 0;

            bool collisioneNemico1 = false;
            bool collisioneNemico2 = false;
            bool collisioneNemico3 = false;

            int numeroProiettiliSparati = 0;

            const int LARGHEZZA_NEMICO = 5;
            const int ALTEZZA_NEMICO = 5;

            const int LARGHEZZA_UTENTE = 5;
            const int ALTEZZA_UTENTE = 4;

            const int LIMITE_MASSIMO_X_SPOSTAMENTO_ASTRONAVE_NEMICA = 70;
            const int LIMITE_MASSIMO_Y_SPOSTAMENTO_ASTRONAVE_NEMICA = 10;
            const int LIMITE_MINIMO_X_SPOSTAMENTO_ASTRONAVE_NEMICA = 1;
            const int LIMITE_MINIMO_Y_SPOSTAMENTO_ASTRONAVE_NEMICA = 1;

            const int LIMITE_MINIMO_X_SPOSTAMENTO_ASTRONAVE_UTENTE = 3;

            const int LIMITE_MASSIMO_Y_SPOSTAMENTO_ASTRONAVE_UTENTE = 28;
            const int LIMITE_MINIMO_Y_SPOSTAMENTO_ASTRONAVE_UTENTE = 10;

            // Ciclo dell'animazione: un fotogramma dell'animazione
            // ad ogni iterazione.
            do
            {
                // Determina lo spostamento dell’astronave nemica1.

                MovimentoOrizzontaleAstronameNemica(ref incAstronave1, ref x1, LIMITE_MASSIMO_X_SPOSTAMENTO_ASTRONAVE_NEMICA, LIMITE_MINIMO_X_SPOSTAMENTO_ASTRONAVE_NEMICA,
                                                    ref incAstronave2, ref x2,
                                                    ref incAstronave3, ref x3);



                MovimentoVerticaleAstronaveNemica(rnd, ref y1, ref y2, ref y3, LIMITE_MASSIMO_Y_SPOSTAMENTO_ASTRONAVE_NEMICA, LIMITE_MINIMO_Y_SPOSTAMENTO_ASTRONAVE_NEMICA);



                // Disegna lo sfondo nel buffer.
                // Per gestire anche il colore ogni elemento del buffer
                // potrebbe essere una tupla con carattere e colore.
                // Andrebbe quindi adattato anche il resto del codice.

                DisegnaSfondoNelBuffer(bufferSchermo, sfondoSchermo);



                AggiornaMovimentoSchermo(sfondoSchermo, rnd);


                // Disegna l’astronave nel buffer.

                DisegnaAstronaviNemiche(astronave, bufferSchermo, y1, x1);
                DisegnaAstronaviNemiche(astronave, bufferSchermo, y2, x2);
                DisegnaAstronaviNemiche(astronave, bufferSchermo, y3, x3);





                // Legge i tasti premuti dall'utente e mantiene solo l'ultimo
                // per evitare che si accumulino comandi, soprattutto se si
                // usano FPS bassi.

                tastoPremuto = new ConsoleKeyInfo(); // Azzera.
                while (Console.KeyAvailable)
                    tastoPremuto = Console.ReadKey(true);



                // Muove la nave dell'utente in base al tasto premuto.                
                MovimentoNaveUtente(tastoPremuto, ref xU, ref yU, LIMITE_MINIMO_X_SPOSTAMENTO_ASTRONAVE_UTENTE, LIMITE_MASSIMO_Y_SPOSTAMENTO_ASTRONAVE_UTENTE,
                                    LIMITE_MINIMO_Y_SPOSTAMENTO_ASTRONAVE_UTENTE, LARGHEZZA_BUFFER);


                // Disegna la nave dell'utente nel buffer.

                DisegnaNaveUtente(bufferSchermo, naveUtente, xU, yU);



                //L'astornave dell'utente si è scontrata con quella del nemico

                // Collisione tra nave utente e nemico1


                ControlloCollisioniNavi(xU, x1, LARGHEZZA_NEMICO, yU, y1, ALTEZZA_NEMICO, ALTEZZA_UTENTE, esplosione, bufferSchermo, LARGHEZZA_BUFFER, LUNGHEZZA_BUFFER,
                                        ref collisioneNemico1, ref numeroViteRimanenti);
                ControlloCollisioniNavi(xU, x2, LARGHEZZA_NEMICO, yU, y2, ALTEZZA_NEMICO, ALTEZZA_UTENTE, esplosione, bufferSchermo, LARGHEZZA_BUFFER, LUNGHEZZA_BUFFER,
                                        ref collisioneNemico2, ref numeroViteRimanenti);
                ControlloCollisioniNavi(xU, x3, LARGHEZZA_NEMICO, yU, y3, ALTEZZA_NEMICO, ALTEZZA_UTENTE, esplosione, bufferSchermo, LARGHEZZA_BUFFER, LUNGHEZZA_BUFFER,
                                        ref collisioneNemico3, ref numeroViteRimanenti);




                GestioneSparoNemici(ref attivazioneSparatoriaAstronaveNemica1, rnd, randomProiettileNemico, ref xProiettileNemico1, ref yProiettileNemico1, x1, y1, astronave);
                GestioneSparoNemici(ref attivazioneSparatoriaAstronaveNemica2, rnd, randomProiettileNemico, ref xProiettileNemico2, ref yProiettileNemico2, x2, y2, astronave);
                GestioneSparoNemici(ref attivazioneSparatoriaAstronaveNemica3, rnd, randomProiettileNemico, ref xProiettileNemico3, ref yProiettileNemico3, x3, y3, astronave);




                MovimentoProiettiliNemici(ref attivazioneSparatoriaAstronaveNemica1, ref yProiettileNemico1, xProiettileNemico1, LUNGHEZZA_BUFFER,
                                          bufferSchermo, ref numeroViteRimanenti, xU, yU, LARGHEZZA_UTENTE, ALTEZZA_UTENTE, esplosione);
                MovimentoProiettiliNemici(ref attivazioneSparatoriaAstronaveNemica2, ref yProiettileNemico2, xProiettileNemico2, LUNGHEZZA_BUFFER,
                                          bufferSchermo, ref numeroViteRimanenti, xU, yU, LARGHEZZA_UTENTE, ALTEZZA_UTENTE, esplosione);
                MovimentoProiettiliNemici(ref attivazioneSparatoriaAstronaveNemica3, ref yProiettileNemico3, xProiettileNemico3, LUNGHEZZA_BUFFER,
                                          bufferSchermo, ref numeroViteRimanenti, xU, yU, LARGHEZZA_UTENTE, ALTEZZA_UTENTE, esplosione);


                //Se l'utente vuole sparare
                GestioneSparoUtente(tastoPremuto, attivo, xProiettili, yProiettili, xU, yU, ref numeroProiettiliSparati);



                MovimentoProiettiliUtente(bufferSchermo, attivo, yProiettili, xProiettili, x1, y1, x2, y2, x3, y3, ref nemiciAbbattuti, ref nemiciDaAbbatterePerPassaggioLivello,
                                          ref livello, ref randomProiettileNemico, esplosione);



                if (numeroProiettiliSparati == NUMERO_MASSIMO_PROIETTILI_UTENTE)
                {
                    numeroProiettiliSparati = 0;
                }

                //Visualizzazione vite.
                DisegnaVite(numeroViteRimanenti, bufferSchermo);


                DisegnaNemiciSconfitti(nemiciAbbattuti, bufferSchermo);



                DisegnaLivelloCorrente(livello, bufferSchermo);


                // Scrive il buffer dello schermo in Console.

                ScriviBufferInConsole(bufferSchermo);


                // Attende 1/30 ms (per 30 PFS).
                Thread.Sleep(TEMPO_FRAME);

                // Se l'utente preme q il programma termina.
            } while (tastoPremuto.KeyChar.ToString().ToUpper() != "Q" && numeroViteRimanenti != 0);

            if (numeroViteRimanenti == 0)
            {
                MostraMessaggioVittoriaOSconfitta("SEI STATO SCONFITTO!");
            }
        }

        /// <summary>
        /// Inizializza lo sfondo stellato
        /// </summary>
        /// <param name="nR">Oggetto random per la posizione delle stelle</param>
        /// <param name="sfondo">Matrice dello sfondo</param>
        /// <param name="buffer">Buffer dello schermo</param>
        static void InizializzazioneSfondo(Random nR, char[,] sfondo, (ConsoleColor colore, char carattere)[,] buffer)
        {
            for (int i = 0; i < buffer.GetLength(0); i++)
            {
                for (int j = 0; j < buffer.GetLength(1); j++)
                {
                    sfondo[i, j] = nR.Next(0, 17) < 1 ? '.' : ' ';
                }
            }
        }

        /// <summary>
        /// Gestione del movimento orizzontale delle tre astronavi nemiche.
        /// Ogni astronave si muove tra un limite minimo e un limite massimo
        /// lungo l'asse X, invertendo la direzione quando raggiunge i bordi.
        /// </summary>
        /// <param name="incrementoAstr1">Indica la direzione di movimento della prima astronave: true verso destra, false verso sinistra.</param>
        /// <param name="xAstr1">Coordinata X della prima astronave.</param>
        /// <param name="limMaxX">Limite massimo consentito sull'asse X.</param>
        /// <param name="limMinX">Limite minimo consentito sull'asse X.</param>
        /// <param name="incrementoAstr2">Indica la direzione di movimento della seconda astronave: true verso destra, false verso sinistra.</param>
        /// <param name="xAstr2">Coordinata X della seconda astronave.</param>
        /// <param name="incrementoAstr3">Indica la direzione di movimento della terza astronave: true verso destra, false verso sinistra.</param>
        /// <param name="xAstr3">Coordinata X della terza astronave.</param>
        static void MovimentoOrizzontaleAstronameNemica(ref bool incrementoAstr1, ref int xAstr1, int limMaxX, int limMinX,
                                                         ref bool incrementoAstr2, ref int xAstr2,
                                                         ref bool incrementoAstr3, ref int xAstr3)
        {
            if (incrementoAstr1)
            {
                xAstr1++;

                if (xAstr1 > limMaxX) // Qui, ad esempio, servono 2 const, perché?

                    incrementoAstr1 = false;
            }
            else
            {
                xAstr1--;
                if (xAstr1 < limMinX)
                    incrementoAstr1 = true;
            }

            if (incrementoAstr2)
            {
                xAstr2++;

                if (xAstr2 > limMaxX) // Qui, ad esempio, servono 2 const, perché?

                    incrementoAstr2 = false;
            }
            else
            {
                xAstr2--;
                if (xAstr2 < limMinX)
                    incrementoAstr2 = true;
            }

            if (incrementoAstr3)
            {
                xAstr3++;

                if (xAstr3 > limMaxX) // Qui, ad esempio, servono 2 const, perché?

                    incrementoAstr3 = false;
            }
            else
            {
                xAstr3--;
                if (xAstr3 < limMinX)
                    incrementoAstr3 = true;
            }
        }

        /// <summary>
        /// Gestione del movimento verticale casuale delle tre astronavi nemiche.
        /// Ogni astronave si muove casualmente tra un limite minimo e un limite massimo
        /// lungo l'asse Y.
        /// </summary>
        /// <param name="nR">Oggetto random per la posizione delle stelle</param>
        /// <param name="yAstr1">Coordinata Y della prima astronave nemica.</param>
        /// <param name="yAstr2">Coordinata Y della seconda astronave nemica.</param>
        /// <param name="yAstr3">Coordinata Y della terza astronave nemica.</param>
        /// <param name="limMax">Limite massimo consentito sull'asse Y.</param>
        /// <param name="limMin">Limite minimo consentito sull'asse Y.</param>
        static void MovimentoVerticaleAstronaveNemica(Random nR, ref int yAstr1, ref int yAstr2, ref int yAstr3, int limMax, int limMin)
        {
            int numeroSpaziDiSpostamentoVerticaleNave1 = nR.Next(0, 25);
            int numeroSpaziDiSpostamentoVerticaleNave2 = nR.Next(0, 25);
            int numeroSpaziDiSpostamentoVerticaleNave3 = nR.Next(0, 25);

            //spostamento verticale nemico1
            if (nR.Next(0, 25) < 3)
            {
                for (int i = 0; i < numeroSpaziDiSpostamentoVerticaleNave1; i++)
                {
                    if (yAstr1 + numeroSpaziDiSpostamentoVerticaleNave1 + 1 < limMax)
                    {
                        yAstr1++;
                    }
                }
            }

            //spostamento verticale nemico2
            if (nR.Next(0, 25) < 3)
            {
                for (int i = 0; i < numeroSpaziDiSpostamentoVerticaleNave2; i++)
                {
                    if (yAstr2 - numeroSpaziDiSpostamentoVerticaleNave2 - 1 > limMin)
                    {
                        yAstr2--;
                    }
                }
            }

            //spostamento verticale nemico3
            if (nR.Next(0, 25) < 3)
            {
                for (int i = 0; i < numeroSpaziDiSpostamentoVerticaleNave3; i++)
                {
                    if (yAstr3 - numeroSpaziDiSpostamentoVerticaleNave3 - 1 > limMin)
                    {
                        yAstr3--;
                    }
                }
            }
        }

        /// <summary>
        /// Questo metodo disegna lo sfondo nel buffer.
        /// </summary>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="sfondo">Matrice dello sfondo da disegnare nel buffer.</param>
        static void DisegnaSfondoNelBuffer((ConsoleColor colore, char carattere)[,] buffer, char[,] sfondo)
        {
            for (int yBuffer = 0; yBuffer < buffer.GetLength(0); yBuffer++)
            {
                for (int xBuffer = 0; xBuffer < buffer.GetLength(1); xBuffer++)
                {
                    buffer[yBuffer, xBuffer] = (ConsoleColor.DarkBlue, sfondo[yBuffer, xBuffer]);
                }
            }
        }

        /// <summary>
        /// Questo metodo aggiorna il movimento delle stelle nello sfondo.
        /// </summary>
        /// <param name="sfondo">Matrice dello sfondo dello schermo.</param>
        /// <param name="nR">Oggetto random.</param>
        static void AggiornaMovimentoSchermo(char[,] sfondo, Random nR)
        {
            for (int nRigheSfondo = sfondo.GetLength(0) - 1; nRigheSfondo > 0; nRigheSfondo--)
            {
                for (int nColonneSfondo = 0; nColonneSfondo < sfondo.GetLength(1); nColonneSfondo++)
                {
                    sfondo[nRigheSfondo, nColonneSfondo] = sfondo[nRigheSfondo - 1, nColonneSfondo];
                }
            }

            for (int nColonneSfondo = 0; nColonneSfondo < sfondo.GetLength(1); nColonneSfondo++)
            {
                sfondo[0, nColonneSfondo] = nR.Next(0, 20) < 1 ? '.' : ' ';
            }
        }

        /// <summary>
        /// Questo metodo disegna nel buffer le astronavi nemiche.
        /// </summary>
        /// <param name="astronaveNemica">Vettore dell'astronave nemica da disegnare.</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="y">Coordinata y iniziale.</param>
        /// <param name="x">Coordinata x iniziale.</param>
        static void DisegnaAstronaviNemiche((int scostamento, string riga)[] astronaveNemica, (ConsoleColor colore, char carattere)[,] buffer, int y, int x)
        {
            for (int yShip = 0; yShip < astronaveNemica.Length; yShip++)
            {
                for (int xShip = 0; xShip < astronaveNemica[yShip].riga.Length; xShip++)
                {
                    buffer[y + yShip, x + xShip + astronaveNemica[yShip].scostamento] = (ConsoleColor.Magenta, astronaveNemica[yShip].riga[xShip]);
                }
            }
        }

        /// <summary>
        /// Questo metodo si occupa del movimento della nave utente entro i limiti del buffer.
        /// </summary>
        /// <param name="tasto">Tasto premuto dall'utente.</param>
        /// <param name="x">Coordinata x della nave utente.</param>
        /// <param name="y">Coordinata y della nave utente.</param>
        /// <param name="limiteMinimoX">Limite minimo consentito sull'asse X.</param>
        /// <param name="limiteMassimoY">Limite massimo consentito sull'asse Y.</param>
        /// <param name="limiteMinimoY">Limite minimo consentito sull'asse Y.</param>
        /// <param name="larghezzaBuffer">Larghezza del buffer.</param>
        static void MovimentoNaveUtente(ConsoleKeyInfo tasto, ref int x, ref int y, int limiteMinimoX, int limiteMassimoY, int limiteMinimoY, int larghezzaBuffer)
        {
            if (tasto.Key == ConsoleKey.LeftArrow && x > limiteMinimoX)
                x--;

            if (tasto.Key == ConsoleKey.RightArrow && (x + 8) < larghezzaBuffer)
                x++;

            if (tasto.Key == ConsoleKey.DownArrow && (y + 4) < limiteMassimoY)
            {
                y++;
            }

            if (tasto.Key == ConsoleKey.UpArrow && y > limiteMinimoY)
            {
                y--;
            }
        }

        /// <summary>
        /// Questo metodo si occupa di disegnare l'astronave utente nel buffer.
        /// </summary>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="nave">Vettore della nave utente.</param>
        /// <param name="x">Coordinata x della nave utente.</param>
        /// <param name="y">Coordinata y della nave utente.</param>
        static void DisegnaNaveUtente((ConsoleColor colore, char carattere)[,] buffer, (int scostamento, string riga)[] nave, int x, int y)
        {
            for (int yN = 0; yN < nave.Length; yN++)
            {
                for (int xN = 0; xN < nave[yN].riga.Length; xN++)
                {
                    buffer[y + yN, x + xN + nave[yN].scostamento] =
                    (ConsoleColor.Cyan, nave[yN].riga[xN]);
                }
            }
        }

        /// <summary>
        /// Questo metodo controlla le collisioni fra le navi
        /// </summary>
        /// <param name="xU">Coordinata x della nave utente.</param>
        /// <param name="x">Coordinata x dell'astronave nemica.</param>
        /// <param name="larghezzaNemico">Larghezza dell'astronave nemica.</param>
        /// <param name="yU">Coordinata y della nave utente.</param>
        /// <param name="y">Coordinata y dell'astronave nemica.</param>
        /// <param name="altezzaNemico">Altezza dell'astronave nemica.</param>
        /// <param name="altezzaUtente">Altezza dell'astronave utente.</param>
        /// <param name="esplosione">Vettore dell'esplosione</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="larghezzaBuffer">Larghezza del buffer dello schermo.</param>
        /// <param name="lunghezzaBuffer">Lunghezza del buffer delo schermo.</param>
        /// <param name="collisioneNemico">Variabile booleana che indica se è stata rilevata una collisione.</param>
        /// <param name="vite">Numero vite rimanenti all'utente.</param>
        static void ControlloCollisioniNavi(int xU, int x, int larghezzaNemico, int yU, int y, int altezzaNemico, int altezzaUtente,
                                           (int scostamento, string riga)[] esplosione, (ConsoleColor colore, char carattere)[,] buffer, int larghezzaBuffer,
                                           int lunghezzaBuffer, ref bool collisioneNemico, ref int vite)
        {
            if (xU < x + larghezzaNemico && xU + larghezzaNemico > x && yU < y + altezzaNemico && yU + altezzaUtente > y)
            {
                for (int yE = 0; yE < esplosione.Length; yE++)
                {
                    for (int xEsplosione = 0; xEsplosione < esplosione[yE].riga.Length; xEsplosione++)
                    {
                        int targetY = yU + yE;
                        int targetX = xU + esplosione[yE].scostamento + xEsplosione;


                        if (targetY >= 0 && targetY < lunghezzaBuffer && targetX >= 0 && targetX < larghezzaBuffer)
                        {
                            buffer[targetY, targetX] = (ConsoleColor.Red, esplosione[yE].riga[xEsplosione]);
                        }
                    }
                }

                if (!collisioneNemico)
                {
                    vite--;
                    collisioneNemico = true;
                }

            }
            else
            {
                collisioneNemico = false;
            }

        }

        /// <summary>
        /// Questo metodo gestisce lo sparo dei proiettili da parte dei nemici.
        /// </summary>
        /// <param name="attivazioneSparatoria">Variabile che indica se un proiettile nemico è attualmente attivo.</param>
        /// <param name="nR">Oggetto random</param>
        /// <param name="randomProiettileNemico">Valore soglia utilizzato per determinare la probabilità di sparo del nemico.</param>
        /// <param name="xProiettile">Coordinata x del proiettile nemico.</param>
        /// <param name="yProiettile">Coordinata y del proiettile nemico.</param>
        /// <param name="x">Coordinata x dell'astronave nemica.</param>
        /// <param name="y">Coordinata y dell'astronave nemica.</param>
        /// <param name="astronave">Vettore dell'astronave nemica.</param>
        static void GestioneSparoNemici(ref bool attivazioneSparatoria, Random nR, int randomProiettileNemico, ref int xProiettile, ref int yProiettile, int x, int y,
                                        (int scostamento, string riga)[] astronave)
        {
            if (!attivazioneSparatoria)
            {
                int sparatoriaAstronave = nR.Next(0, 35);

                if (sparatoriaAstronave >= randomProiettileNemico)
                {
                    attivazioneSparatoria = true;
                    xProiettile = x + astronave[4].scostamento;   // centro nave utente
                    yProiettile = y + (astronave.Length + 1);
                }
            }
        }

        /// <summary>
        /// Il proiettile viene spostato verso il basso nel buffer e viene controllata l'eventuale collisione con la nave utente.
        /// </summary>
        /// <param name="attivazioneSparatoria">Variabile che indica se il proiettile nemico è attivo.</param>
        /// <param name="yProiettile">Coordinata Y del proiettile nemico.</param>
        /// <param name="xProiettile">Coordinata X del proiettile nemico.</param>
        /// <param name="lunghezzaBuffer">Lunghezza del buffer.</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="vite">Vite rimanenti.</param>
        /// <param name="xU">Coordinata X nave utente.</param>
        /// <param name="yU">Coordinata Y nave utente.</param>
        /// <param name="larghezzaUtente">Larghezza nave utente.</param>
        /// <param name="altezzaUtente">Altezza nave utente.</param>
        /// <param name="esplosione">Vettore dell'esplosione.</param>
        static void MovimentoProiettiliNemici(ref bool attivazioneSparatoria, ref int yProiettile, int xProiettile, int lunghezzaBuffer,
                                              (ConsoleColor colore, char carattere)[,] buffer, ref int vite, int xU, int yU, int larghezzaUtente, int altezzaUtente,
                                              (int scostamento, string riga)[] esplosione)
        {
            if (attivazioneSparatoria)
            {
                yProiettile++;

                if (yProiettile == lunghezzaBuffer - 1)
                {
                    attivazioneSparatoria = false;
                }
                else
                {
                    buffer[yProiettile, xProiettile] = (ConsoleColor.Red, '*');
                    buffer[yProiettile + 1, xProiettile] = (ConsoleColor.Red, '*');
                }

                //Se il nemico1 ha colpito l'utente
                if (xProiettile >= xU && xProiettile <= xU + larghezzaUtente && yProiettile >= yU && yProiettile <= yU + altezzaUtente)
                {
                    vite--;
                    for (int yEsplosione = 0; yEsplosione < esplosione.Length; yEsplosione++)
                    {
                        for (int xEsplosione = 0; xEsplosione < esplosione[yEsplosione].riga.Length; xEsplosione++)
                        {
                            buffer[yU + yEsplosione,
                                xU + esplosione[yEsplosione].scostamento + xEsplosione] =
                            (ConsoleColor.Yellow, esplosione[yEsplosione].riga[xEsplosione]);
                        }

                    }

                    attivazioneSparatoria = false;
                }

            }
        }

        /// <summary>
        /// Questo metodo gestisce lo sparo dell'utente quando preme il tasto dello spazio.
        /// </summary>
        /// <param name="tasto">Tasto premuto dall'utente.</param>
        /// <param name="attivo">Vettore che indica quali proiettili sono attualmente attivi.</param>
        /// <param name="xProiettili">Vettore contenente le coordinate X dei proiettili.</param>
        /// <param name="yProiettili">Vettore contenente le coordinate Y dei proiettili.</param>
        /// <param name="xU">Coordinata X dell'utente.</param>
        /// <param name="yU">Coordinata Y dell'utente.</param>
        /// <param name="proiettiliSparati">Indice proiettile sparato.</param>
        static void GestioneSparoUtente(ConsoleKeyInfo tasto, bool[] attivo, int[] xProiettili, int[] yProiettili, int xU, int yU, ref int proiettiliSparati)
        {
            if ((tasto.Modifiers & ConsoleModifiers.Shift) != 0 || tasto.Key == ConsoleKey.Spacebar)
            {
                attivo[proiettiliSparati] = true;
                xProiettili[proiettiliSparati] = xU + 2;
                yProiettili[proiettiliSparati] = yU - 1;

                proiettiliSparati++;

                if (proiettiliSparati >= attivo.Length)
                {
                    proiettiliSparati = 0;
                }
            }
        }

        /// <summary>
        /// Gestisce il movimento dei proiettili sparati dall'utente.
        /// </summary>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="attivo">Vettore che indica quali proiettili sono attualmente attivi.</param>
        /// <param name="yProiettili">Vettore contenente le coordinate Y dei proiettili.</param>
        /// <param name="xProiettili">ettore contenente le coordinate X dei proiettili.</param>
        /// <param name="x1">Coordinata X della prima astronave nemica.</param>
        /// <param name="y1">Coordinata Y della prima astronave nemica.</param>
        /// <param name="x2">Coordinata X della seconda astronave nemica.</param>
        /// <param name="y2">Coordinata Y della seconda astronave nemica.</param>
        /// <param name="x3">Coordinata X della terza astronave nemica.</param>
        /// <param name="y3">Coordinata Y della terza astronave nemica.</param>
        /// <param name="nemiciAbbattuti">Numero totale di nemici abbattuti dal giocatore.</param>
        /// <param name="nemiciDaAbbatterePerPassaggioLivello">Numero di nemici da abbattere necessario per passare al livello successivo.</param>
        /// <param name="livello">Livello attuale del gioco.</param>
        /// <param name="randomProiettileNemico">Valore utilizzato per determinare la probabilità di sparo dei nemici.</param>
        /// <param name="esplosione">Vettore dell'esplosione.</param>
        static void MovimentoProiettiliUtente((ConsoleColor colore, char carattere)[,] buffer, bool[] attivo, int[] yProiettili, int[] xProiettili, int x1, int y1,
                                               int x2, int y2, int x3, int y3, ref int nemiciAbbattuti, ref int nemiciDaAbbatterePerPassaggioLivello, ref int livello,
                                               ref int randomProiettileNemico, (int scostamento, string riga)[] esplosione)
        {
            for (int i = 0; i < attivo.Length; i++)
            {
                if (attivo[i])
                {
                    yProiettili[i]--;

                    if (yProiettili[i] <= 0)
                    {
                        attivo[i] = false;
                    }
                    else
                    {
                        buffer[yProiettili[i], xProiettili[i]] = (ConsoleColor.DarkGreen, '|');
                    }

                    ControlloCollisioneNemico(yProiettili, xProiettili, x1, y1, ref nemiciAbbattuti, ref nemiciDaAbbatterePerPassaggioLivello, ref livello,
                                              ref randomProiettileNemico, attivo, buffer, esplosione, i);

                    ControlloCollisioneNemico(yProiettili, xProiettili, x2, y2, ref nemiciAbbattuti, ref nemiciDaAbbatterePerPassaggioLivello, ref livello,
                                              ref randomProiettileNemico, attivo, buffer, esplosione, i);

                    ControlloCollisioneNemico(yProiettili, xProiettili, x3, y3, ref nemiciAbbattuti, ref nemiciDaAbbatterePerPassaggioLivello, ref livello,
                                              ref randomProiettileNemico, attivo, buffer, esplosione, i);

                }
            }
        }


        /// <summary>
        /// Controlla la collisione tra un proiettile del giocatore e un'astronave nemica.
        /// </summary>
        /// <param name="yProiettili">Vettore delle coordinate Y dei proiettili.</param>
        /// <param name="xProiettili">Vettore delle coordinate X dei proiettili.</param>
        /// <param name="x">Coordinata X dell'astronave nemica.</param>
        /// <param name="y">Coordinata Y dell'astronave nemica.</param>
        /// <param name="nemiciAbbattuti">Numero totale di nemici abbattuti dal giocatore.</param>
        /// <param name="nemiciDaAbbatterePerPassaggioLivello">Numero di nemici necessari per passare al livello successivo.</param>
        /// <param name="livello">Livello attuale dell'utente.</param>
        /// <param name="randomProiettileNemico">Valore che regola la probabilità di sparo dei nemici.</param>
        /// <param name="attivo">Vettore che indica quali proiettili sono attualmente attivi.</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        /// <param name="esplosione">Vettore dell'esplosione.</param>
        /// <param name="i"> Indice del proiettile da controllare.</param>
        static void ControlloCollisioneNemico(int[] yProiettili, int[] xProiettili, int x, int y, ref int nemiciAbbattuti, ref int nemiciDaAbbatterePerPassaggioLivello,
                                              ref int livello, ref int randomProiettileNemico, bool[] attivo, (ConsoleColor colore, char carattere)[,] buffer,
                                              (int scostamento, string riga)[] esplosione, int i)
        {
            if (xProiettili[i] >= x && xProiettili[i] <= x + 5 &&
                            yProiettili[i] >= y && yProiettili[i] <= y + 5)
            {
                nemiciAbbattuti++;

                if (nemiciAbbattuti == nemiciDaAbbatterePerPassaggioLivello)
                {
                    nemiciDaAbbatterePerPassaggioLivello += 5;
                    livello++;

                    if (randomProiettileNemico - 2 > 0)
                    {
                        randomProiettileNemico -= 2;
                    }
                    else
                    {
                        MostraMessaggioVittoriaOSconfitta("HAI SUPERATO TUTTI I LIVELLI!");
                    }

                }

                attivo[i] = false;

                // esplosione
                for (int yE = 0; yE < esplosione.Length; yE++)
                {
                    for (int xE = 0; xE < esplosione[yE].riga.Length; xE++)
                    {
                        buffer[y + yE,
                            x + esplosione[yE].scostamento + xE] =
                            (ConsoleColor.Magenta, esplosione[yE].riga[xE]);
                    }
                }
            }
        }

        /// <summary>
        /// Questo metodo scrive nel buffer dello schermo le vite che rimangono all'utente.
        /// </summary>
        /// <param name="numeroViteRimanenti">Numero vite rimaneti.</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        static void DisegnaVite(int numeroViteRimanenti, (ConsoleColor colore, char carattere)[,] buffer)
        {
            string vite = "Vite: " + numeroViteRimanenti;

            //Visualizzazione vite
            for (int i = 0; i < vite.Length; i++)
            {
                buffer[0, i] = (ConsoleColor.White, vite[i]);
            }
        }

        /// <summary>
        /// Questo metodo scrive all'interno del buffer il numero di nemici sconfitti.
        /// </summary>
        /// <param name="nemiciAbbattuti">Nemici che l'utente ha sconfitto.</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        static void DisegnaNemiciSconfitti(int nemiciAbbattuti, (ConsoleColor colore, char carattere)[,] buffer)
        {
            string sconfitti = "Nemici sconfitti: " + nemiciAbbattuti;
            int posizioneXNemiciSconfitti = 80 - sconfitti.Length;

            // Visualizzazione nemici sconfitti
            for (int i = 0; i < sconfitti.Length; i++)
            {
                buffer[0, posizioneXNemiciSconfitti + i] =
                    (ConsoleColor.White, sconfitti[i]);
            }
        }

        /// <summary>
        /// Questo metodo scrive all'interno del buffer il livello corrente in cui si trova l'utente.
        /// </summary>
        /// <param name="livello">Livello attuale dell'utente.</param>
        /// <param name="buffer">Buffer dello schermo.</param>
        static void DisegnaLivelloCorrente(int livello, (ConsoleColor colore, char carattere)[,] buffer)
        {
            string stringaLivelloCorrente = "Livello corrente: " + livello;
            int posizioneXlivelloCorrente = 80 - stringaLivelloCorrente.Length;

            // Visualizzazione livello corrente
            for (int i = 0; i < stringaLivelloCorrente.Length; i++)
            {
                buffer[1, posizioneXlivelloCorrente + i] =
                    (ConsoleColor.White, stringaLivelloCorrente[i]);
            }
        }

        /// <summary>
        /// Questo metodo scrive il buffer dello schermo all'interno della console.
        /// </summary>
        /// <param name="buffer">Buffer dello schermo da scrivere in console.</param>
        static void ScriviBufferInConsole((ConsoleColor colore, char carattere)[,] buffer)
        {
            Console.SetCursorPosition(0, 0);

            ConsoleColor coloreCorrente = ConsoleColor.Black;

            for (int yb = 0; yb < buffer.GetLength(0); yb++)
            {
                for (int xb = 0; xb < buffer.GetLength(1); xb++)
                {
                    if (buffer[yb, xb].colore != coloreCorrente)
                    {
                        Console.ForegroundColor = buffer[yb, xb].colore;
                        coloreCorrente = buffer[yb, xb].colore;
                    }

                    Console.Write(buffer[yb, xb].carattere);
                }

                if (yb != buffer.GetLength(0) - 1)
                    Console.WriteLine();
            }
        }

        /// <summary>
        /// Questo metodo mostra il messaggio di vittoria o di sconfitta.
        /// </summary>
        /// <param name="stringa">Stringa da mostrare.</param>
        static void MostraMessaggioVittoriaOSconfitta(string stringa)
        {
            Console.Clear();
            Console.SetCursorPosition(25, 15);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(stringa);
            Thread.Sleep(3000);
            return;
        }


    }
}