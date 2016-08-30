
$(document).ready(function () {

    $("#recommendMe").click(function () {
        var userid = $("#userId").val();
        var table = $("#recomMovies").DataTable({
            ajax: {
                url: "/api/Movies/recommendations/" + userid,
                dataSrc: ""
            },
            columns: [
                {
                    data: "title",
                    render: function (data, type, recommend) {
                        return "<a target='_blank' href='http://www.imdb.com/title/tt" + recommend.movieId + "'>" + recommend.title + "</a>";
                    }
                },

                {
                    data: "genre",
                    render: function (data) {
                        return data;
                    }
                },
                 {
                     data: "year",
                     render: function (data) {
                         return data;
                     }
                 }
            ]
        });
    })

});
