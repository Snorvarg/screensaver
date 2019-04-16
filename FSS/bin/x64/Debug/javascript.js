// Strictly check for source code errors in the function.
"use strict";

/* Den här ritar ut kartan 3x3 gånger, för att visa att den gränslösa kartan funkar. (ja, man kan gå 'runt' världen åt alla håll)
 * Har också fixat de sista buggarna, så nu äter färgerna varann i denna ordningen: r->g->b->r.
 * Den här versionen har en svält också; ifall den inte hittar någon granne att sluka på, så svälter den istället. 
 * De har också en Åldersprick! Ju äldre de är, desto vitare blir den.
 * De har en elderPower prick topleft! Ju äldre de är, och ju mer excess de har, ju mer energi får de. Resultatet är kort och gott att 
 *  de står emot attacker genom att de inte går att svälta ut. 
 * När de sprider sig får de ge bort excess ifall de har mer än tillräckligt. Så att de i sin tur kan sprida sig. 
 *
 * Om doSpreadFood = true så slängs det ut nya slumpade rutor efterhand.
 * Har fixat inställningar för ett flertal grejer, kolla nedan.
 * Har fixat så att de slumpar ut vilken ordning de letar ibland grannarna när de ska sprida sig. 
 * (De söker igenom alla 8 tills de hittar en.)
 * Har fixat buggen där de drar iväg i långa linjer! Det var att den gav bort _all_ sin excess, som ibland kunde vara mer än 1000, 
 * och då spred den sig vidare så klart.
 * Har lagt till en OverJoy bild som verkligen lyser upp supermakterna.
 * 
 * Efter lite inställningar av värden och en buggfix, så uppstår verkligen självuppehållande supermakter av elders! De kan tom expandera ifall de är omgivna av lite yngre krafter. Krig kan också härja omkring en supermakt, medan den kan hålla stånd ett bra tag.
 * 
 * Superpowerkrafter som har nästan full energi kan ha en viss sannolikhet för att pumpa ut mängder av energi till en svag granne, för att orsaka ett 'utbrott'. 
 */
  
// Idé: Om man ska lägga upp det som demo, gör man det mha. Cake. Där finns bra stöd o säkert färdiga skripts som packar o mögar ner javascriptfiler så de blir 'oläsliga'. 
  
  
// Ta skärmens vidd, få skala baserat på gamepad-upplösning. Så får vi samma storlek på både tv och gamepad.
var CurrentWidth = window.innerWidth;
var scale = CurrentWidth/1920; // =1 ifall tvn kör 1920.
var ScreenWidth = 1920 * scale;
var ScreenHeight = 1080 * scale;
      
// Radianer: Pi = 180 grader.
var oneDegree = Math.PI / 180;
    
// canvasens 2d yta. Sätts i init().
var ctx = null;

// Våra fina tidsvariabler.
var Now = 0;
var Past = 0;
var ElapsedTime = 0;

var theLevel = null;
var levelWidth = 64;
var levelHeight = 64;

var squareSize = 12;

var repeatMapXTimes=3;
var repeatMapYTimes=3;

var theRotator = 0;
var rotationSpeed = 0.1;
var xCenterOfRotation = squareSize * levelWidth * repeatMapXTimes * scale / 2;
var yCenterOfRotation = squareSize * levelHeight * repeatMapYTimes * scale / 2;

var doSpreadFood = true;
var spreadProbability = 0.99995; // Värde mellan 0 och 1, där 0.999 är en chans på tusen, medan 0.5 är 1 chans på 2.
var turnInToFoodIfThisWeakOnly = 2; // Om de är starkare än så här förvandlas de inte till mat. (ersätts alltså inte med ny färg)

// Så här mycket måste den samla på sig innan den kan sprida sig till en svagare granne.
var excessLimitBeforeSpread = 16;

// Den kan inte ta över en annan ruta om inte alla grundfärgerna är lägre än detta värde.
var mustBeThisWeak = 8;

// Om de inte hittar nån granne att sluka av, så svälter den så här mycket istället.
var starveAmount = 1.25;

// Ifall excess växer väldigt mycket, och de blir gamla, så får de förmågan att leva på luft. :) 
// Detta är vad de får ifall de är max-gamla och excess > 200. 
var maxElderPower = 36 * 2;

// Den här kommer att räknas ut senare i varje loop för att se till att de 'äter' lika snabbt oavsett framerate.
var eatSpeed = 20;

var canGiveAwayExcessToNeighbour = true;
var maxGiveAwayExcess = 16;

// NOTE: Fel namn här, eftersom effekten blev att rutor med excess kan ge bort till andra starka rutor, och därmed sprider sig maktcentran fortare.
// NOTE: Varför kan det fortfarande uppstå 3x3 rutor som håller sig vid liv men inte sprider sig? (De försöker, men de svälter ihjäl utanför rätt fort)
var maxGiveAwayExcessToWeak = 90;  // Den kan ge bort denna större summa ifall det finns en väldigt svag granne, med en låg sannolikhet.
var giveAwayToMaxThisStrong = 200;   // Den får inte ge bort till någon starkare än så här.

var imgOverJoy = null;

var totalPreloadCount = 0;
var finishedLoadingCount = 0;

// Called as soon as the page has loaded.
function init()
{
  // Kör endast ifall vi är på nintendos grejer.
  if (typeof(nwf) !="undefined")
  {
    var c=document.getElementById("canvas");
    var tvDisplay = nwf.display.DisplayManager.getInstance().getTVDisplay();
    //tvDisplay.load('Guac-A-MoleTV.html');
  }
  
  // Denna kod tar sig in i canvas id och anger storlek.
  var canvasSize = document.getElementById('canvas');		
  canvasSize.height = ScreenHeight;
  canvasSize.width = ScreenWidth;
  
  var c=document.getElementById("canvas");
  ctx=c.getContext("2d");

  // TODO: Den laddar inte in bilden...
  // imgOverJoy = PreloadImage("/img/OverJoy.png");
  imgOverJoy = new Image();
    
  ComputeTime(); // Så de inte skapas i tidens begynnelse. (0)
  CreateLevel();  
  
  // Starta spelet!
  GameLoop();
}

function PreloadImage(url)
{
  totalPreloadCount++;
  console.log('totalPreloadCount: ' + totalPreloadCount);
  
  var img = new Image();
  img.onload = function() {
    // Vi måste rita ut bilden en gång, för att trigga uppackningen av .png filen in i grafikminnet. Det tar tid på stora bilder och skulle
    // orsaka ett hack första gången den används i spelet. 
    // Använder javascripts lite magiska förmåga med 'local context', dvs. variabeln img pekar faktiskt på bilden. 
    ctx.drawImage(img,0,0,10,10);
    
    finishedLoadingCount++;
    
    console.log('finishedLoadingCount: ' + finishedLoadingCount);
  };
  
  img.src = url;

  return img;
}

// The infurious gameloop
function GameLoop()
{
  ComputeTime();

  // Just make sure graphics are loaded.
  if(finishedLoadingCount >= totalPreloadCount)
  {
    update();
    draw(ctx);
  }
  else
  {
    // console.log(finishedLoadingCount + " < " + totalPreloadCount);
  }
  
  window.requestAnimationFrame(GameLoop);
}

// Logiken händer här. 
var old = 0;
function update()
{
  theRotator += rotationSpeed;
  
  // För att undvika att de ständigt sprider sig topleft först, så slumpar vi ordningen i varje loop.
  var arrayOfOrder = new Array();
  var doneCount = 0;
  while(doneCount < 8)
  {
    var val = Math.floor(Math.random() * 8);
    
    if(arrayOfOrder.indexOf(val) == -1)
    {
      arrayOfOrder.push(val);
      doneCount++;
    }
  }
  
  for(var x=0;x<levelWidth;x++)
  {
    for(var y=0;y<levelHeight;y++)
    {
      // Fixa fram de 8 grannarna. 
      // Förkortningar för grannar; topleft=tl, bottom = b, osv.
      var neighbours = new Array();
      neighbours.push(GetLevelSquare(x-1,y-1)); //tl
      neighbours.push(GetLevelSquare(x,y-1)); //t
      neighbours.push(GetLevelSquare(x+1,y-1)); //tr
      neighbours.push(GetLevelSquare(x-1,y)); //l
      neighbours.push(GetLevelSquare(x+1,y)); //r
      neighbours.push(GetLevelSquare(x-1,y+1)); //bl
      neighbours.push(GetLevelSquare(x,y+1)); //b
      neighbours.push(GetLevelSquare(x+1,y+1)); //br
      
      var here = theLevel[x][y];
      
      // Bestäm typ, helt enkelt den färg den har mest av. (VAD KRÅNGLIGT!)
      // Helt svart,helt vit o gråskalor kommer inte att göra nånting. 
      // Men nästa loop kommer den vita att vara äten av, och då gör den nåt.
      var type = '';
      if(here.r > here.g && here.r > here.b)
      {
        type = 'r';
      }
      else if(here.g > here.b && here.g > here.r)
      {
        type = 'g';
      }
      else if(here.b > here.g && here.b > here.r)
      {
        type = 'b';
      }
      
      var iSluked = false;
      if(type != '')
      {
        for(var i=0;i<neighbours.length;i++)
        {
          // Den äter andra färger i denna kedjan: r->g->b->r
          switch(type)
          {
            case 'r':
              if(neighbours[i].g > 0)
              {
                if(neighbours[i].g >= eatSpeed)
                {
                  neighbours[i].g -= eatSpeed;
                  here.r += eatSpeed;
                }
                else
                {
                  here.r += neighbours[i].g;
                  neighbours[i].g = 0;
                }
                iSluked = true;
              }
            break;
            case 'g':
              if(neighbours[i].b > 0)
              {
                if(neighbours[i].b >= eatSpeed)
                {
                  neighbours[i].b -= eatSpeed;
                  here.g += eatSpeed;
                }
                else
                {
                  here.g += neighbours[i].b;
                  neighbours[i].b = 0;
                }              
                iSluked = true;
              }
            break;
            case 'b':
              if(neighbours[i].r > 0)
              {
                if(neighbours[i].r >= eatSpeed)
                {
                  neighbours[i].r -= eatSpeed;
                  here.b += eatSpeed;
                }
                else
                {
                  here.b += neighbours[i].r;
                  neighbours[i].r = 0;
                }
                iSluked = true;
              }
            break;
          }
        }
      }
      
//TODO: Fel här, here.powerOfAge blir inte uppdaterad på välfödda rutor, eller nåt. (De dör i lugn o ro med fet powerOfAge)
      if(iSluked == false)
      {
        // Stackarn, helt hungrig.
        if(here.excess > 0)
        {
          here.excess -= starveAmount;
          
          // Uråldriga med mycket access får en slags yberstatus.
          if(here.excess > 50 && Now - here.created > 5000)
          {
            here.powerOfAge = Now - here.created;
            if(here.powerOfAge > 10000)
              here.powerOfAge = 10000;
            here.powerOfAge = (here.powerOfAge / 10000);
            
            var excess = here.excess;
            if(excess > 100)
              excess = 100;
              
            // powerOfAge kan vara max 1, och here.excess/100 <= 1, så han kan komma nära maxElderPower.
            here.powerOfAge = maxElderPower * here.powerOfAge * (excess/100);
            
            if(type == 'r')
              here.r += here.powerOfAge;
            if(type == 'g')
              here.g += here.powerOfAge;
            if(type == 'b')
              here.b += here.powerOfAge;
          }
          else
          {
            here.powerOfAge = 0;
          }
          
          // Överfeta ger bort lite av sin excess till sina vänner. 
          if(here.excess > 100 && canGiveAwayExcessToNeighbour == true)
          {
            // arrayOfOrder är alltid slumpad, så varje loop får vi en annan granne.
            var n = neighbours[arrayOfOrder[0]];
            
            if(type == 'r' && n.r > n.g && n.r > n.b)
            {
              // Den är röd, och det är grannen med, så ge den sin excess.
              if(n.r <= giveAwayToMaxThisStrong)
              {
                n.r += maxGiveAwayExcessToWeak;
                here.excess -= maxGiveAwayExcessToWeak;
              }
              else
              {
                n.r += maxGiveAwayExcess;
                here.excess -= maxGiveAwayExcess;
              }
            }
            else if(type == 'g' && n.g > n.b && n.g > n.r)
            {
              if(n.g <= giveAwayToMaxThisStrong)
              {
                n.g += maxGiveAwayExcessToWeak;
                here.excess -= maxGiveAwayExcessToWeak;
              }
              else
              {
                n.g += maxGiveAwayExcess;
                here.excess -= maxGiveAwayExcess;
              }
            }
            else if(type == 'b' && n.b > n.g && n.b > n.r)
            {
              if(n.b <= giveAwayToMaxThisStrong)
              {
                n.b += maxGiveAwayExcessToWeak;
                here.excess -= maxGiveAwayExcessToWeak;
              }
              else
              {
                n.b += maxGiveAwayExcess;
                here.excess -= maxGiveAwayExcess;
              }
            }
          }
        }
        else
        {
          here.r -= starveAmount;
          here.g -= starveAmount;
          here.b -= starveAmount;
          
          here.powerOfAge = 0;
        }
      }
      
      // debug
      if(x==7 && y == 7 && old != here.r + here.g + here.b)
      {
        //console.log(here);
        old = here.r + here.g + here.b;
      }
      
      // Lägg överskott/underskott i excess, så den kan använda det för att sprida sig.
      // Varje ruta rättar till sitt underskott när det blir den rutans tur. En förenkling, men det är alright.
      if(here.r > 255)
      {
        here.excess += here.r - 255;
        here.r = 255;
      }
      if(here.r < 0)
      {
        here.r = 0;
      }
      if(here.g > 255)
      {
        here.excess += here.g - 255;
        here.g = 255;
      }
      if(here.g < 0)
      {
        here.g = 0;
      }
      if(here.b > 255)
      {
        here.excess += here.b - 255;
        here.b = 255;
      }
      if(here.b < 0)
      {
        here.b = 0;
      }
      
      if(here.excess > excessLimitBeforeSpread)
      {
        // Spread to any black/dead neighbour. Välj en på måfå, om den inte e bra, testar vi en annan nästa loop.
        for(var i=0;i<neighbours.length;i++)
        {
          // Plocka ut grannen. 
          var n = neighbours[arrayOfOrder[i]];

          var amount = here.excess;
          if(amount > 255)
            amount = 255;
          
          // Vi ändrar bara i existerande objekts variabler, skapar inte nytt objekt, för vi vet inte riktigt var grannen är. :)
          // (Skulle behöva uppdatera pekaren i theLevel)
          if(n.r < mustBeThisWeak && n.g < mustBeThisWeak && n.b < mustBeThisWeak)
          {
            switch(type)
            {
              case 'r':
                n.r = amount;
              break;
              case 'g':
                n.g = amount;
              break;
              case 'b':
                n.b = amount;
              break;
            }
            
            n.created = Now;
            n.excess = 0;   // Vi kan inte ta över excess från den döda rutan.
            n.powerOfAge = 0;
            
            here.excess -= amount; // Vi har ju gett bort en del.
            
            // En supernod får ge bort excess!
            if(here.excess > 255)
            {
              n.excess = 255;
              here.excess -= 255;
            }
            
            break;
          }
        }
      }

      // Varje ruta har en chans på 1000 att få en ny färg.
      if(Math.random() > spreadProbability && doSpreadFood)
      {
        if(here.r < turnInToFoodIfThisWeakOnly && here.g < turnInToFoodIfThisWeakOnly && here.b < turnInToFoodIfThisWeakOnly)
        {
          theLevel[x][y] = RandomizeMapSquare();
        }
      }
    }
  }
}

// Returnerar rutan på position x,y. Wrappar x o y, så om de är utanför banan, så wrappas de rätt.
function GetLevelSquare(x,y)
{
  var pos = WrapPosition(x,y);
  
  return theLevel[pos.x][pos.y];
}

// Den här 'wrappar' koordinaterna, så om x=-1 så blir x=levelWidth-1, dvs. i _andra_ änden av banan.
function WrapPosition(x,y)
{
  if(x < 0)
    x += (levelWidth);
  if(x > levelWidth - 1)
    x -= (levelWidth);
    
  if(y < 0)
    y += (levelHeight);
  if(y > levelHeight - 1)
    y -= (levelHeight);
    
  var pos = new Object();
  pos.x = x;
  pos.y = y;
  
  return pos;
}

function CreateLevel()
{
  theLevel = Create2DArray(levelWidth,levelHeight);
  
  for(var x=0;x<levelWidth;x++)
  {
    for(var y=0;y<levelHeight;y++)
    {
      // Stoppa in ett objekt med r,g,b.
      theLevel[x][y] = RandomizeMapSquare();
    }
  }
}

function RandomizeMapSquare()
{
  // Man föds alltid nu, med väldigt lite på fötterna.
  var age = Now;
  var excess = 0;
  var r = Math.floor(Math.random() * 256);
  var g = Math.floor(Math.random() * 256);
  var b = Math.floor(Math.random() * 256);
  
  if(Math.random() < 0.1)
  {
    // En viss procent skapas uråldriga dock.
    age = Now - 20000;
    excess = 5000;
    
    var c = Math.floor(Math.random() * 3);
    if(c==0)
      r = 255;
    if(c==1)
      g = 255;
    if(c==2)
      b = 255;      
  }

  return CreateMapSquare(
    r,
    g,
    b,
    excess,
    age);
}

function CreateMapSquare(r,g,b,excess,created)
{
  var rgb = new Object();
  rgb.r = r;
  rgb.g = g;
  rgb.b = b;
  rgb.excess = excess;
  rgb.created = created;
  
  return rgb;
}

// Will create a 2Dimensional array with the given width and height. Usage: arr[5][12] = 8;
function Create2DArray(w,h)
{
  var arr = new Array(w); // Create an array w wide.
  for (var y = 0; y < w; y++) 
  {
    arr[y] = new Array(h);  // Each array has a h high column.
  }
  
  return arr;
}

// Räknar ut tiden som gått sen förra gången den anropades. 
function ComputeTime()
{
  Past = Now;
  Now = (new Date()).getTime();
  ElapsedTime = Now - Past;
  
  //console.log(ElapsedTime);
  
  // När man trycker på WiiUs Home-button kan vi inte upptäcka det. (Feature request finnes) 
  // Så istället kollar vi efter en orimligt lång frame, o nollar tiden ifall det händer. 
  if (ElapsedTime >= 300)
  {
    Past = Now;
    ElapsedTime = 0;
  }
}

// RITA FUNKTIONER

// Ritar spelet i ctx. 
function draw(ctx)
{
  //Tömmer allt vid varje draw genom att rita över allt med en svart bakgrund.
  ctx.fillStyle="black"; 
  ctx.fillRect(0,0,ScreenWidth,ScreenHeight);

  // Spara undan offset, rotation, scale, så vi kan återställa sedan.
  ctx.save();
    ctx.translate(-ScreenWidth / 8, -ScreenHeight / 2);
    ctx.translate(xCenterOfRotation,yCenterOfRotation);
    ctx.rotate(oneDegree * theRotator);
    ctx.translate(-xCenterOfRotation,-yCenterOfRotation);

    // Rita kartan 3x3 gånger, för att visa att den gränslösa metoden fungerar.
    for(var x=0;x<repeatMapXTimes;x++)
    {
      ctx.save();
        for(var y=0;y<repeatMapYTimes;y++)
        {
          drawMap();
          ctx.translate(0,levelHeight * (squareSize + 1) * scale);
        }
      ctx.restore();    
      ctx.translate(levelWidth * (squareSize + 1) * scale,0);    
    }    
  ctx.restore();
}

function drawMap()
{
  for(var x=0;x<levelWidth;x++)
  {
    ctx.save();
    ctx.translate(x * (squareSize + 1) * scale, 0);
    
    for(var y=0;y<levelHeight;y++)
    {
      // Fortsätt flytta squareSize pixels i varje loop.
      ctx.translate(0, (squareSize + 1) * scale);
      
      var here = theLevel[x][y];
      var r = Math.round(here.r);
      var g = Math.round(here.g);
      var b = Math.round(here.b);
      
      // Endast de levande förtjänar att ritas.
      if(r + g + b > 0)
      {
        var powerOfAge = Now - here.created;
        if(powerOfAge > 10000)
          powerOfAge = 10000;
        powerOfAge = (powerOfAge / 10000);
        
        ctx.fillStyle="rgba("+r+","+g+","+b+",1.0)";
        ctx.fillRect(0, 0, squareSize * scale, squareSize * scale);
        
        // Rita ut ålderspricken ifall den blivit stark nog.
        if(powerOfAge > 0.3)
        {
          var tl = (squareSize * 0.35) * scale;
          var br = (squareSize * 0.45) * scale;
          
          ctx.fillStyle="rgba(255,255,255," + powerOfAge + ")";
          ctx.fillRect(tl, tl, br, br);
        }
        
        if(here.powerOfAge > maxElderPower / 10)
        {
          var tl = (squareSize * 0.15) * scale;
          var br = (squareSize * 0.25) * scale;
          
          ctx.fillStyle="rgba(255,255,255," + (here.powerOfAge / maxElderPower) + ")";
          ctx.fillRect(tl, tl, br, br);
        }
        
        if(powerOfAge > 0.9 || here.powerOfAge > maxElderPower * 0.9)
        {
          ctx.globalAlpha = 0.3;
          ctx.drawImage(imgOverJoy, -3, -3, squareSize * scale + 6, squareSize * scale + 6);
          ctx.globalAlpha = 1.0;
        }
      }
    }
    ctx.restore();
  }
}