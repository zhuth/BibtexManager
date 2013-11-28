// ==UserScript==
// @name           cnki bibtex generator
// @description    cnki bibtex generator
// @namespace      http://tianhua.me/
// @auth           zhuth
// @version        0.11
// @license        Public Domain
// @include		   http://*.cnki.*/*.aspx*
// ==/UserScript==

var x = document.getElementsByClassName("picShow")[0].getElementsByTagName("a");
var journal = x[1].innerText;
var xt = ''; for(var i in x) xt += x[i].innerText; 
var year = parseInt(xt.substring(xt.indexOf('年')-4));
var number = parseInt(xt.substring(xt.indexOf('年')+1));
var title = document.getElementsByTagName('h1')[0].innerText;
var authors = document.getElementsByClassName('author')[0].innerText.split('【')[1].substring(3).trim().split('；');
while (authors[authors.length - 1] == '') authors.pop();
authors = authors.join(', ');
var bibtex = "@article {" + authors.substring(0, 3) + year + title + ",\r\n" + 
			"\tauthor = {" + authors + "},\r\n" +
			"\ttitle = {" + title + "},\r\n" +
			"\tjournal = {" + journal + "},\r\n" +
			"\tyear = {" + year + "},\r\n" +
			"\tnumber = {" + number + "}\r\n" +
			"}\r\n";
			
document.getElementsByClassName('author')[0].innerHTML += '<textarea id="bibtex" style="width: 300px; height: 100px;" onmouseover="document.getElementById(\'bibtex\').select()">' + bibtex + '</textarea>';
