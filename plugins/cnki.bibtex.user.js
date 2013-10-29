// ==UserScript==
// @name           cnki bibtex generator
// @description    cnki bibtex generator
// @namespace      http://tianhua.me/
// @auth           zhuth
// @version        0.1
// @license        Public Domain
// @include		   http://*.cnki.*/detail.aspx*
// ==/UserScript==

var x=document.getElementsByClassName("picShow")[0].getElementsByTagName("a");
var journal = x[1].innerText;
var year = parseInt(x[4].innerText);
var number = parseInt(x[4].innerText.substring(x[4].innerText.indexOf('年')+1));
var title = document.getElementsByTagName('h1')[0].innerText;
var authors = document.getElementsByClassName('author')[0].innerText.split('【')[1].substring(3).trim().split('；');
while (authors[authors.length - 1] == '') authors.pop();
authors = authors.join(', ');
var bibtex = "@article {article" + title + ",\r\n" + 
			"\tauthor = {" + authors + "},\r\n" +
			"\ttitle = {" + title + "},\r\n" +
			"\tjournal = {" + journal + "},\r\n" +
			"\tyear = {" + year + "},\r\n" +
			"\tnumber = {" + number + "}\r\n" +
			"}\r\n";
			
document.getElementsByClassName('author')[0].innerHTML += '<textarea id="bibtex" style="width: 300px; height: 100px;" onmouseover="document.getElementById(\'bibtex\').select()">' + bibtex + '</textarea>';